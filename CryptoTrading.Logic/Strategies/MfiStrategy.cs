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
    public class MfiStrategy : IStrategy
    {
        private readonly IIndicator _mfiIndicator;
        private TrendDirection _lastTrend = TrendDirection.Short;

        public MfiStrategy(IIndicatorFactory indicatorFactory, IOptions<MfiStrategyOptions> options)
        {
            _mfiIndicator = indicatorFactory.GetMfiIndicator(options.Value.MfiWeight);
        }

        public int CandleSize => 1;
        public Task<TrendDirection> CheckTrendAsync(string tradingPair, List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var mfiValue = _mfiIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            Console.WriteLine($"Mfi indicator value: {mfiValue}");

            if (mfiValue < 0)
            {
                return Task.FromResult(TrendDirection.None);
            }
            if (mfiValue > 0 && mfiValue <= 20)
            {
                if (_lastTrend == TrendDirection.Short)
                {
                    _lastTrend = TrendDirection.Long;
                    return Task.FromResult(_lastTrend);
                }
            }
            if (mfiValue >= 80)
            {
                if (_lastTrend == TrendDirection.Long)
                {
                    _lastTrend = TrendDirection.Short;
                    return Task.FromResult(_lastTrend);
                }
            }

            return Task.FromResult(TrendDirection.None);
        }
    }
}
