using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Strategies.Interfaces;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class MacdStrategy : IStrategy
    {
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private readonly IIndicator _signalEmaIndicator;

        private TrendDirection _lastTrend = TrendDirection.Short;
        private int _persistenceBuyCount = 1;
        private int _persistenceSellCount = 1;
        private decimal _lastBuyPrice;
        private decimal? _lastMacd;

        public MacdStrategy(IOptions<MacdStrategyOptions> options, IIndicatorFactory indicatorFactory)
        {
            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.LongWeight);
            _signalEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.Signal);
        }

        public int CandleSize => 1;

        public async Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var emaDiffValue = shortEmaValue - longEmaValue;
            var signalEmaValue = Math.Round(_signalEmaIndicator.GetIndicatorValue(emaDiffValue).IndicatorValue, 4);
            var macdValue = Math.Round(emaDiffValue - signalEmaValue, 4);
            //var macdLongThreshold = currentCandle.ClosePrice * (decimal)0.00088;
            //var macdShortThreshold = currentCandle.ClosePrice * (decimal)0.00045;

            Console.WriteLine($"DateTs: {currentCandle.StartDateTime:s}; " +
                              $"MACD: {macdValue};" +
                              $"Close price: {currentCandle.ClosePrice}; ");

            if (!_lastMacd.HasValue)
            {
                _lastMacd = macdValue;
                return await Task.FromResult(TrendDirection.None);
            }

            var diffPreviousMacd = Math.Abs(_lastMacd.Value - macdValue);
            if (_lastTrend == TrendDirection.Short)
            {
                if (/*_lastMacd < (decimal)-8.4*/ macdValue < 0 && diffPreviousMacd > (decimal)0.15 && macdValue > _lastMacd)
                {
                    _lastMacd = macdValue;
                    _lastTrend = TrendDirection.Long;
                    _lastBuyPrice = currentCandle.ClosePrice;
                }
                else
                {
                    _persistenceBuyCount = 1;
                    _lastMacd = macdValue;
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if (_lastTrend == TrendDirection.Long)
            {
                if (/*_lastMacd > (decimal)4.4*/ macdValue > 0 && diffPreviousMacd > (decimal)0.15 && macdValue < _lastMacd /*|| currentCandle.ClosePrice < _lastBuyPrice * (decimal)0.9975*/)
                {
                    _lastMacd = macdValue;
                    if (_persistenceSellCount > 2)
                    {
                        _lastTrend = TrendDirection.Short;
                    }
                    else
                    {
                        _persistenceSellCount++;
                        return await Task.FromResult(TrendDirection.None);
                    }
                }
                else
                {
                    _persistenceSellCount = 1;
                    _lastMacd = macdValue;
                    return await Task.FromResult(TrendDirection.None);
                }
            }


            //if (_lastTrend == TrendDirection.Short)
            //{
            //    if (macdValue > signalEmaValue)
            //    {
            //        if (_persistenceBuyCount > 1)
            //        {
            //            _lastTrend = TrendDirection.Long;
            //            _persistenceBuyCount = 1;
            //            _lastBuyPrice = currentCandle.ClosePrice;
            //        }
            //        else
            //        {
            //            _persistenceBuyCount++;
            //            return await Task.FromResult(TrendDirection.None);
            //        }
            //    }
            //    else
            //    {
            //        return await Task.FromResult(TrendDirection.None);
            //    }
            //}
            //else if (_lastTrend == TrendDirection.Long)
            //{
            //    if (macdValue < signalEmaValue)
            //    {
            //        if (_persistenceSellCount > 2 || currentCandle.ClosePrice < _lastBuyPrice * (decimal)0.9975)
            //        {
            //            _lastTrend = TrendDirection.Short;
            //            _persistenceSellCount = 1;
            //        }
            //        else
            //        {
            //            _persistenceSellCount++;
            //            return await Task.FromResult(TrendDirection.None);
            //        }
            //    }
            //    else
            //    {
            //        return await Task.FromResult(TrendDirection.None);
            //    }
            //}


            return await Task.FromResult(_lastTrend);
        }
    }
}
