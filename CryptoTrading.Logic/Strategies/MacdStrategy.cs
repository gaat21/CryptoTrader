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
        private decimal _maxOrMinMacd;
        private decimal _minMacd;
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
                              $"Close price: {currentCandle.ClosePrice};");

            if (!_lastMacd.HasValue)
            {
                _lastMacd = macdValue;
                _lastClosePrice = currentCandle.ClosePrice;
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

                var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_stopTrading == false 
                    && macdValue < _options.BuyThreshold 
                    && diffPreviousMacd < -(decimal)1.0
                    && macdValue > _lastMacd
                    && currentCandle.ClosePrice > _lastClosePrice)
                {
                    _macdRate = (1 - Math.Round(_minMacdClosePrice / _maxMacdClosePrice, 2)) * 100;
                    Console.WriteLine($"MaxMacd: {_maxMacdClosePrice}; MinMacd: {_minMacdClosePrice}; Rate: {_macdRate}%");

                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    if (_macdRate <= (decimal)3.0 || _macdRate > (decimal)5.5)
                    {
                        //_stopTrading = true;
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

                var stopPercentage = 1 - (_macdRate - (decimal) 0.4) / 100;
                var profitPercentage = 1 + (_macdRate + (decimal)0.4) / 100; //(decimal) 1.038;
                var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_lastMacd > macdValue
                    && diffPreviousMacd > (decimal)1.0
                    && (currentCandle.ClosePrice > _lastBuyPrice * profitPercentage
                    || currentCandle.ClosePrice < _lastBuyPrice * stopPercentage))
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
    }
}
