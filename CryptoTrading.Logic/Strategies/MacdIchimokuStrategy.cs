using System;
using System.Collections.Generic;
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
    public class MacdIchimokuStrategy : IStrategy
    {
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;

        private readonly IIndicator _ichimokuCloudIndicator;
        private readonly IIndicator _ema200Indicator;

        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private decimal? _lastMacd;
        private bool _stopTrading;
        private decimal _maxOrMinMacd;
        private decimal _lastClosePrice;
        private readonly FixedSizedQueue<decimal> _last10Ema200ClosePriceRate;

        public MacdIchimokuStrategy(IOptions<MacdStrategyOptions> options, 
                                    IIndicatorFactory indicatorFactory)
        {
            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.LongWeight);

            _ema200Indicator = indicatorFactory.GetEmaIndicator(200);

            _ichimokuCloudIndicator = indicatorFactory.GetIchimokuCloud();

            _last10Ema200ClosePriceRate = new FixedSizedQueue<decimal>(10);
        }

        public int DelayInCandlePeriod => 1;

        public async Task<TrendDirection> CheckTrendAsync(string tradingPair, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var macdValue = Math.Round(shortEmaValue - longEmaValue, 4);

            var ichimokuCloudValue = _ichimokuCloudIndicator.GetIndicatorValue(currentCandle);
            var ema200Value = _ema200Indicator.GetIndicatorValue(currentCandle).IndicatorValue;

            Console.WriteLine($"DateTs: {currentCandle.StartDateTime:s}; " +
                              $"SSA: {ichimokuCloudValue.IchimokuCloud?.SenkouSpanAValue}; " +
                              $"SSB: {ichimokuCloudValue.IchimokuCloud?.SenkouSpanBValue}; " +
                              $"TS: {ichimokuCloudValue.IchimokuCloud?.TenkanSenValue}; " +
                              $"KS: {ichimokuCloudValue.IchimokuCloud?.KijunSenValue}; " +
                              $"EMA200: {ema200Value}; " +
                              $"MACD: {macdValue}; " +
                              $"MinMaxMacd: {_maxOrMinMacd}; " +
                              $"Close price: {currentCandle.ClosePrice};");

            if (!_lastMacd.HasValue || _lastMacd == 0)
            {
                _lastMacd = macdValue;
                return await Task.FromResult(TrendDirection.None);
            }

            if (ichimokuCloudValue.IchimokuCloud == null)
            {
                _lastMacd = macdValue;
                return await Task.FromResult(TrendDirection.None);
            }

            var ssa = ichimokuCloudValue.IchimokuCloud.SenkouSpanAValue;
            var ssb = ichimokuCloudValue.IchimokuCloud.SenkouSpanBValue;
            //var ts = ichimokuCloudValue.IchimokuCloud.TenkanSenValue;
            //var ks = ichimokuCloudValue.IchimokuCloud.KijunSenValue;

            //var ichimokuCloudMin = new List<decimal> { ssa, ssb, ts, ks}.Min();
            var ichimokuCloudSsList = new List<decimal>{ ssa, ssb };

            _last10Ema200ClosePriceRate.Enqueue(Math.Round(currentCandle.ClosePrice / ema200Value , 4));
            
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

                var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_stopTrading == false
                    && macdValue < 0
                    && diffPreviousMacd < -(decimal)0.2
                    && macdValue > _lastMacd
                    && currentCandle.ClosePrice > _lastClosePrice
                    && currentCandle.ClosePrice > ema200Value
                    && currentCandle.ClosePrice <= ichimokuCloudSsList.Max()
                    && currentCandle.ClosePrice >= ichimokuCloudSsList.Min()
                    && _last10Ema200ClosePriceRate.GetItems().Count(a => a > (decimal)1.0) > 5)
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    _lastTrend = TrendDirection.Long;
                    _maxOrMinMacd = 0;
                    _lastBuyPrice = currentCandle.ClosePrice;
                    _stopTrading = true;
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

                var stopPercentage = (decimal) 0.98;
                var profitPercentage = (decimal) 1.03;
                if (currentCandle.ClosePrice <= _lastBuyPrice * stopPercentage)
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    _lastTrend = TrendDirection.Short;
                    _maxOrMinMacd = 0;
                    _stopTrading = true;
                    return await Task.FromResult(_lastTrend);
                }

                var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_lastMacd > macdValue
                    && diffPreviousMacd > (decimal)0.4
                    //&& currentCandle.ClosePrice < ema200Value
                    //&& currentCandle.ClosePrice > _lastBuyPrice * profitPercentage
                    )
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;

                    if (currentCandle.ClosePrice <= _lastBuyPrice * profitPercentage)
                    {
                        return await Task.FromResult(TrendDirection.None);
                    }

                    Console.WriteLine($"Stop percentage: {stopPercentage}; Profit percentage: {profitPercentage}");
                    _lastTrend = TrendDirection.Short;
                    _maxOrMinMacd = 0;
                    _stopTrading = true;
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
    }
}
