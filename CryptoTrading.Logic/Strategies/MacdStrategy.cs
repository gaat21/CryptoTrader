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
        private int _candleCount = 1;
        private int _lastTradeTimeCount = 1;
        private int _delayPercentage = 90;
        private decimal _lastClosePrice;

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
                              $"MACD: {macdValue};\t" +
                              $"PeekMACD: {_maxOrMinMacd};\t" +
                              $"LastTradeCount: {_lastTradeTimeCount};\t" +
                              $"Close price: {currentCandle.ClosePrice};");

            if (!_lastMacd.HasValue)
            {
                _lastMacd = macdValue;
                _lastClosePrice = currentCandle.ClosePrice;
                return await Task.FromResult(TrendDirection.None);
            }

            // wait 1 hour
            if (_candleCount <= 60)
            {
                _candleCount++;
                return await Task.FromResult(TrendDirection.None);
            }

            _lastTradeTimeCount++;
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
                var calcPercentage = (_delayPercentage - _lastTradeTimeCount) / 10000;
                if (_lastTradeTimeCount >= _delayPercentage)
                {
                    _lastSellPrice = 0;
                }
                var buyPercentage = 1 - calcPercentage;
                if (_stopTrading == false 
                    && macdValue < _options.BuyThreshold 
                    && diffPreviousMacd < -(decimal)1.0
                    && macdValue > _lastMacd
                    && currentCandle.ClosePrice > _lastClosePrice
                    && (_lastSellPrice == 0 
                    || currentCandle.ClosePrice < _lastSellPrice * buyPercentage))
                {
                    _lastTrend = TrendDirection.Long;
                    _maxOrMinMacd = 0;
                    _lastBuyPrice = currentCandle.ClosePrice;
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

                var stopPercentage = (decimal) 0.97;
                var profitPercentage = (decimal) 1.038;
                var diffPreviousMacd = Math.Abs(_maxOrMinMacd - macdValue);
                if (_lastMacd > macdValue
                    && diffPreviousMacd > (decimal)1.0
                    && (currentCandle.ClosePrice > _lastBuyPrice * profitPercentage
                    || currentCandle.ClosePrice < _lastBuyPrice * stopPercentage))
                {
                    _lastTrend = TrendDirection.Short;
                    _maxOrMinMacd = 0;
                    _stopTrading = true;
                    _lastMacd = macdValue;
                    _lastSellPrice = currentCandle.ClosePrice;
                    _lastTradeTimeCount = 1;
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
    }
}
