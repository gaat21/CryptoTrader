using System;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Strategies.Interfaces;
using CryptoTrading.Logic.Strategies.Models;
using CryptoTrading.Logic.Utils;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class CustomBtcStrategy : IStrategy
    {
        private readonly IExchangeProvider _exchangeProvider;
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private readonly IIndicator _signalEmaIndicator;

        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private decimal _maxOrMinMacd;
        private decimal _minMacd;
        private decimal _minWarmupMacd;
        private decimal _maxMacd;
        private decimal _maxMacdClosePrice;
        private decimal _minMacdClosePrice;
        private decimal? _lastMacd;
        private readonly MacdStrategyOptions _options;
        private bool _stopTrading;
        private decimal _macdRate;
        private bool _macdSwitch;
        private int _candleCount = 1;
        private decimal _lastClosePrice;
        private MacdDirection _macdDirection = MacdDirection.GreaterThanZero;
        private readonly FixedSizedQueue<decimal> _volumenQueue;
        private decimal _profitEstimationRate;
        private readonly IIndicator _ichimokuCloudIndicator;

        public CustomBtcStrategy(IOptions<MacdStrategyOptions> options, IIndicatorFactory indicatorFactory, IExchangeProvider exchangeProvider)
        {
            _exchangeProvider = exchangeProvider;
            _options = options.Value;
            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(_options.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(_options.LongWeight);
            _signalEmaIndicator = indicatorFactory.GetEmaIndicator(_options.Signal);

            _ichimokuCloudIndicator = indicatorFactory.GetIchimokuCloud();

            _volumenQueue = new FixedSizedQueue<decimal>(100);
        }

        public int DelayInCandlePeriod => 180;

        public async Task<TrendDirection> CheckTrendAsync(string tradingPair, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var macdValue = Math.Round(shortEmaValue - longEmaValue, 4);
            var signalEmaValue = Math.Round(_signalEmaIndicator.GetIndicatorValue(macdValue).IndicatorValue, 4);
            var histogramValue = Math.Round(macdValue - signalEmaValue, 4);
            
            var ichimokuCloudValue = _ichimokuCloudIndicator.GetIndicatorValue(currentCandle);

            var ssa = ichimokuCloudValue.IchimokuCloud?.SenkouSpanAValue;
            var ssb = ichimokuCloudValue.IchimokuCloud?.SenkouSpanBValue;

            DepthModel depth = null;
            if (ichimokuCloudValue.IchimokuCloud != null)
            {
                depth = await _exchangeProvider.GetDepth(tradingPair);
            }

            var bid = depth?.Bids.First(f => f.Quantity == depth.Bids.Max(b => b.Quantity));
            var ask = depth?.Asks.First(f => f.Quantity == depth.Asks.Max(b => b.Quantity));
            var bidPrice = bid?.Price ?? Math.Round(currentCandle.ClosePrice, 4);
            var askPrice = ask?.Price ?? Math.Round(currentCandle.ClosePrice, 4);

            var bidPercentage = Math.Round((currentCandle.ClosePrice / bidPrice) * (decimal)100.0 - (decimal)100.0, 2);
            var askPercentage = Math.Round((currentCandle.ClosePrice / askPrice) * (decimal)100.0 - (decimal)100.0, 2);

            Console.WriteLine($"DateTs: {currentCandle.StartDateTime:s}; " +
                              $"MACD Value: {macdValue}; " +
                              $"SSA/SSB: {ssa}/{ssb}; " +
                              $"Bids price/qty/%: {bid}/{bidPercentage}%; " +
                              $"Asks price/qty/%: {ask}/{askPercentage}%; " +
                              $"Close price: {Math.Round(currentCandle.ClosePrice, 4)};");

            _volumenQueue.Enqueue(currentCandle.Volume);

            if (!_lastMacd.HasValue || _lastMacd == 0)
            {
                _lastMacd = macdValue;
                _lastClosePrice = currentCandle.ClosePrice;
                _macdDirection = macdValue < 0 ? MacdDirection.LessThanZero : MacdDirection.GreaterThanZero;
                return await Task.FromResult(TrendDirection.None);
            }

            if (_lastMacd < 0 && macdValue >= 0)
            {
                if (_macdSwitch)
                {
                    _maxMacd = 0;
                }
                else
                {
                    _macdSwitch = true;
                }
            }

            if (_lastMacd > 0 && macdValue <= 0)
            {
                if (_macdSwitch)
                {
                    _minMacd = 0;
                }
                else
                {
                    _macdSwitch = true;
                }
            }

            if (macdValue < 0 && macdValue < _minMacd)
            {
                _minMacd = macdValue;
                _minMacdClosePrice = currentCandle.ClosePrice;
            }

            if (macdValue > 0 && macdValue > _maxMacd)
            {
                _maxMacd = macdValue;
                _maxMacdClosePrice = currentCandle.ClosePrice;
            }

            // wait 0.5 hour
            if (_candleCount <= 30)
            {
                _candleCount++;
                if (macdValue < 0 && macdValue < _minWarmupMacd)
                {
                    _minWarmupMacd = macdValue;
                }
                Console.WriteLine($"Min warmup Macd: {_minWarmupMacd}");
                return await Task.FromResult(TrendDirection.None);
            }

            if (_lastTrend == TrendDirection.Short)
            {
                if (macdValue > 0 && _stopTrading)
                {
                    _stopTrading = false;
                }

                if (macdValue < 0 && macdValue < _lastMacd)
                {
                    _maxOrMinMacd = macdValue;
                }

                //var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_stopTrading == false
                    && macdValue < _options.BuyThreshold
                    && currentCandle.ClosePrice > ssa
                    && currentCandle.ClosePrice > ssb
                    && currentCandle.CandleType == CandleType.Green
                    //&& diffPreviousMacd < -(decimal)0.2
                    && macdValue > _lastMacd
                    && currentCandle.ClosePrice > _lastClosePrice)
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;

                    _profitEstimationRate = CheckProfitPrecentageAsync(depth, currentCandle.ClosePrice);
                    if (_profitEstimationRate < 0)
                    {
                        return await Task.FromResult(TrendDirection.None);
                    }

                    _lastTrend = TrendDirection.Long;
                    _maxOrMinMacd = 0;
                    _lastBuyPrice = currentCandle.ClosePrice;
                }
                else
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if (_lastTrend == TrendDirection.Long)
            {
                if (macdValue > 0 && macdValue > _lastMacd)
                {
                    _maxOrMinMacd = macdValue;
                }

                if (macdValue < 0)
                {
                    _maxOrMinMacd = 0;
                }

                var stopPercentage = 1 - _profitEstimationRate / (decimal)100.0;//1 - (_macdRate - (decimal)0.4) / 100; //(decimal) 0.97;
                var profitPercentage = 1 + (_profitEstimationRate + (decimal)0.4) / (decimal)100.0; //1 + (_macdRate + (decimal)0.4) / 100; //(decimal) 1.038;
                //var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_lastMacd > macdValue
                    //&& diffPreviousMacd > (decimal)1.0
                    && currentCandle.ClosePrice > _lastBuyPrice * profitPercentage
                    || currentCandle.ClosePrice < _lastBuyPrice * stopPercentage)
                {
                    Console.WriteLine($"Stop percentage: {stopPercentage}; Profit percentage: {profitPercentage}");
                    _lastTrend = TrendDirection.Short;
                    _maxOrMinMacd = 0;
                    _stopTrading = true;
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                }
                else
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(TrendDirection.None);
                }
            }

            return await Task.FromResult(_lastTrend);
        }

        private decimal CheckProfitPrecentageAsync(DepthModel depth, decimal closePrice)
        {
            //var bidsWall = depth.Bids.First(f => f.Quantity == depth.Bids.Max(b => b.Quantity));
            var asksWall = depth.Asks.First(f => f.Quantity == depth.Asks.Max(b => b.Quantity));

            var bidsSum = depth.Bids.Sum(s => s.Quantity);
            var asksSum = depth.Asks.Sum(s => s.Quantity);

            var profitEstimation = (asksWall.Price / closePrice - 1) * 100;
            Console.WriteLine($"Profit estimation: {Math.Round(profitEstimation, 4)}%");
            if (profitEstimation > _options.ProfitEstimationRate
                && bidsSum > asksSum)
            {
                return Math.Round(profitEstimation, 4);
            }

            return -1;
        }
    }
}
