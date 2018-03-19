using System;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Strategies.Interfaces;
using CryptoTrading.Logic.Utils;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class IchimokuCloudBtc2Strategy : IStrategy
    {
        private readonly IIndicator _ichimokuCloudIndicator;

        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private readonly IIndicator _rsiIndicator;

        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private int _candleCount = 1;
        private decimal _lastMacd;
        private decimal _lastClosePrice;
        private readonly FixedSizedQueue<decimal> _last10Macd;
        private int _delayCount = 1;
        private decimal _maxOrMinMacd;
        private bool _stopTrading;
        private readonly FixedSizedQueue<decimal> _prevClosePrices;

        public int DelayInCandlePeriod => 150;

        public IchimokuCloudBtc2Strategy(IOptions<MacdStrategyOptions> options, IIndicatorFactory indicatorFactory)
        {
            _ichimokuCloudIndicator = indicatorFactory.GetIchimokuCloud();
            _rsiIndicator = indicatorFactory.GetRsiIndicator(7);

            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.LongWeight);

            _last10Macd = new FixedSizedQueue<decimal>(10);
            _prevClosePrices = new FixedSizedQueue<decimal>(25);
        }

        public async Task<TrendDirection> CheckTrendAsync(string tradingPair, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var macdValue = Math.Round(shortEmaValue - longEmaValue, 4);

            var rsiValue = _rsiIndicator.GetIndicatorValue(currentCandle).IndicatorValue;

            var ichimokuCloudValue = _ichimokuCloudIndicator.GetIndicatorValue(currentCandle);

            var ssa = ichimokuCloudValue.IchimokuCloud?.SenkouSpanAValue;
            var ssb = ichimokuCloudValue.IchimokuCloud?.SenkouSpanBValue;
            var ts = ichimokuCloudValue.IchimokuCloud?.TenkanSenValue;
            var ks = ichimokuCloudValue.IchimokuCloud?.KijunSenValue;

            Console.WriteLine($"DateTs: {currentCandle.StartDateTime:s}; " +
                              $"SSA: {ssa}; " +
                              $"SSB: {ssb}; " +
                              $"TS: {ts}; " +
                              $"KS: {ks}; " +
                              $"MACD: {macdValue}; " +
                              $"MinMaxMacd: {_maxOrMinMacd}; " +
                              $"RSI: {rsiValue}; " +
                              $"Close price: {Math.Round(currentCandle.ClosePrice, 4)};");

            // wait 1 hour
            if (_candleCount <= 60)
            {
                _candleCount++;
                _last10Macd.Enqueue(macdValue);
                _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                return await Task.FromResult(TrendDirection.None);
            }

            if (_lastTrend == TrendDirection.Short)
            {
                if ((currentCandle.ClosePrice < ssa 
                    && currentCandle.ClosePrice < ssb)
                    && _stopTrading)
                {
                    _stopTrading = false;
                }

                if (macdValue < 0 && macdValue < _lastMacd)
                {
                    _maxOrMinMacd = macdValue;
                }

                if ( currentCandle.ClosePrice > ssa
                    && currentCandle.ClosePrice > ssb
                    && currentCandle.CandleType == CandleType.Green
                    && _stopTrading == false
                    && macdValue < (decimal)20.0 
                    && macdValue > (decimal)-20.0
                    && _last10Macd.GetItems().All(a => a < macdValue)
                    )
                {
                    _lastMacd = macdValue;
                    _last10Macd.Enqueue(macdValue);
                    _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                    _lastTrend = TrendDirection.Long;
                    _lastBuyPrice = currentCandle.ClosePrice;
                    _lastClosePrice = currentCandle.ClosePrice;
                }
                else
                {
                    _last10Macd.Enqueue(macdValue);
                    _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if (_lastTrend == TrendDirection.Long)
            {
                if (_delayCount < 10)
                {
                    Console.WriteLine($"DelayCount: {_delayCount}");
                    _delayCount++;
                    _last10Macd.Enqueue(macdValue);
                    _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(TrendDirection.None);
                }

                if (macdValue > 0 && macdValue > _lastMacd)
                {
                    _maxOrMinMacd = macdValue;
                }

                if (macdValue < 0)
                {
                    _maxOrMinMacd = 0;
                }

                var stopPercentage = (decimal)0.95;
                var profitPercentage = (decimal) 1.02;
                var diffPreviousMacd = _maxOrMinMacd < 0 ? 0 : Math.Abs(_maxOrMinMacd - macdValue);
                var diffLastClosePricePercentage = (currentCandle.ClosePrice / _lastClosePrice) * (decimal)100.0 - 100;
                if (diffLastClosePricePercentage <= (decimal)-0.75)
                {
                    _last10Macd.Enqueue(macdValue);
                    _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                    _lastMacd = macdValue;
                    _delayCount = 1;
                    _lastTrend = TrendDirection.Short;
                    _stopTrading = true;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(_lastTrend);
                }

                if (currentCandle.ClosePrice < _lastBuyPrice * stopPercentage)
                {
                    _last10Macd.Enqueue(macdValue);
                    _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                    _lastMacd = macdValue;
                    _delayCount = 1;
                    _lastTrend = TrendDirection.Short;
                    _stopTrading = true;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(_lastTrend);
                }

                if (
                    macdValue > 0 &&
                    _lastMacd > 0 &&
                    _lastMacd > macdValue &&
                    diffPreviousMacd > (decimal)1.0 &&
                    currentCandle.ClosePrice > _lastBuyPrice * profitPercentage)
                {
                    _last10Macd.Enqueue(macdValue);
                    _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                    _lastMacd = macdValue;
                    _delayCount = 1;
                    _lastTrend = TrendDirection.Short;
                    _stopTrading = true;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(_lastTrend);
                }
                
                _last10Macd.Enqueue(macdValue);
                _prevClosePrices.Enqueue(currentCandle.ClosePrice);
                _lastMacd = macdValue;
                _lastClosePrice = currentCandle.ClosePrice;
                return await Task.FromResult(TrendDirection.None);
            }

            return await Task.FromResult(_lastTrend);
        }
    }
}
