using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Strategies.Interfaces;

namespace CryptoTrading.Logic.Strategies
{
    public class RsiStrategy : IStrategy
    {
        private TrendDirection _lastTrend = TrendDirection.Short;
        private readonly IIndicator _rsiIndicator;
        public int CandleSize => 1;

        public RsiStrategy(IIndicatorFactory indicatorFactory)
        {
            _rsiIndicator = indicatorFactory.GetRsiIndicator(14);
        }

        public Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var rsiValue = _rsiIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            Console.WriteLine($"Rsi value: {rsiValue}");
            
            if (rsiValue < 30 && rsiValue > 0)
            {
                if (_lastTrend == TrendDirection.Long)
                {
                    return Task.FromResult(TrendDirection.None);
                }
                _lastTrend = TrendDirection.Long;
                return Task.FromResult(_lastTrend);
            }

            if (rsiValue > 70)
            {
                if (_lastTrend == TrendDirection.Short)
                {
                    return Task.FromResult(TrendDirection.None);
                }

                _lastTrend = TrendDirection.Short;
                return Task.FromResult(_lastTrend);
            }

            return Task.FromResult(TrendDirection.None);
        }
    }
}
