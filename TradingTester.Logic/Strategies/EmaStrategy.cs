using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TradingTester.Logic.Indicators.Interfaces;
using TradingTester.Logic.Models;
using TradingTester.Logic.Options;
using TradingTester.Logic.Strategies.Interfaces;

namespace TradingTester.Logic.Strategies
{
    public class EmaStrategy : IStrategy
    {
        private TrendDirection _lastTrend = TrendDirection.Short;
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;

        public EmaStrategy(IIndicatorFactory emaIndicatorFactory, IOptions<EmaStrategyOptions> emaOptions)
        {
            var emaIndicatorFactory1 = emaIndicatorFactory;
            var emaOptions1 = emaOptions.Value;
            _shortEmaIndicator = emaIndicatorFactory1.GetIndicator(emaOptions1.ShortWeight);
            _longEmaIndicator = emaIndicatorFactory1.GetIndicator(emaOptions1.LongWeight);
        }

        public async Task<TrendDirection> CheckTrendAsync(decimal price)
        {
            var trendDirection = TrendDirection.Short;

            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(price);
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(price);
            Console.WriteLine($"Short EMA value: ${shortEmaValue}; Long EMA value: ${longEmaValue}");
            if (shortEmaValue > longEmaValue)
            {
                trendDirection = TrendDirection.Long;
            }
            if (trendDirection != _lastTrend)
            {
                _lastTrend = trendDirection;
                return await Task.FromResult(trendDirection);
            }

            return await Task.FromResult(TrendDirection.None);
        }
    }
}
