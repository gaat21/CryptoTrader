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
        private decimal _lastBuyPrice;
        private decimal _lastSellPrice;
        private decimal _maxOrMinMacd;
        private decimal? _lastMacd;
        private readonly MacdStrategyOptions _options;
        private bool _stopTrading;
        private bool _firstBuyFinished = false;
        private int _candleCount = 1;
        private int _stopCount = 1;

        public MacdStrategy(IOptions<MacdStrategyOptions> options, IIndicatorFactory indicatorFactory)
        {
            _options = options.Value;
            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(_options.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(_options.LongWeight);
            _signalEmaIndicator = indicatorFactory.GetEmaIndicator(_options.Signal);
        }

        public int CandleSize => 1;

        public async Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var emaDiffValue = shortEmaValue - longEmaValue;
            var signalEmaValue = Math.Round(_signalEmaIndicator.GetIndicatorValue(emaDiffValue).IndicatorValue, 4);
            var macdValue = Math.Round(emaDiffValue - signalEmaValue, 4);

            Console.WriteLine($"DateTs: {currentCandle.StartDateTime:s}; " +
                              $"MACD: {macdValue}; " +
                              $"Signal: {signalEmaValue}; " +
                              $"Histrogram: {macdValue - signalEmaValue}; " +
                              $"PeekMACD: {_maxOrMinMacd}; " +
                              $"Close price: {currentCandle.ClosePrice}; ");

            if (!_lastMacd.HasValue)
            {
                _lastMacd = macdValue;
                return await Task.FromResult(TrendDirection.None);
            }

            // wait 1 hour
            if (_candleCount <= 60)
            {
                _candleCount++;
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

                _lastMacd = macdValue;
                var diffPreviousMacd = Math.Abs(_maxOrMinMacd - macdValue);
                if (_stopTrading == false 
                    && macdValue < _options.BuyThreshold 
                    && diffPreviousMacd > (decimal)0.5
                    && (_lastSellPrice == 0 || currentCandle.ClosePrice < _lastSellPrice * (decimal)0.99))
                {
                    //if (!_firstBuyFinished)
                    //{
                    //    if (_lastBuyPrice != 0 || currentCandle.ClosePrice < _lastBuyPrice * (decimal)0.985)
                    //    {
                    //        _firstBuyFinished = true;
                    //    }
                    //    else
                    //    {
                    //        _stopTrading = true;
                    //        _lastBuyPrice = currentCandle.ClosePrice;
                    //        return await Task.FromResult(TrendDirection.None);
                    //    }
                    //}
                   
                    _lastTrend = TrendDirection.Long;
                    _maxOrMinMacd = 0;
                    _lastBuyPrice = currentCandle.ClosePrice;
                }
                else
                {
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

                decimal stopPercentage = 1 - _stopCount * (decimal)0.03;
                var diffPreviousMacd = Math.Abs(_maxOrMinMacd - macdValue);
                if (_maxOrMinMacd > _options.SellThreshold 
                    && _lastMacd > macdValue
                    && diffPreviousMacd > (decimal)0.5
                    && currentCandle.ClosePrice > _lastBuyPrice * (decimal)1.02
                    || currentCandle.ClosePrice < _lastBuyPrice * stopPercentage)
                {
                    //if (currentCandle.ClosePrice < _lastBuyPrice * stopPercentage)
                    //{
                    //    _stopCount++;
                    //}

                    _lastTrend = TrendDirection.Short;
                    _maxOrMinMacd = 0;
                    _stopTrading = true;
                    _lastMacd = macdValue;
                    _lastSellPrice = currentCandle.ClosePrice;
                }
                else
                {
                    _lastMacd = macdValue;
                    return await Task.FromResult(TrendDirection.None);
                }
            }

            return await Task.FromResult(_lastTrend);
        }
    }
}
