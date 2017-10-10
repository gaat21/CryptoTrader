using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using TradingTester.Logic.Indicators.Interfaces;
using TradingTester.Logic.Models;
using TradingTester.Logic.Options;
using TradingTester.Logic.Strategies.Interfaces;

namespace TradingTester.Logic.Strategies
{
    public class CustomStrategy : IStrategy
    {
        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;

        public CustomStrategy(IIndicatorFactory emaIndicatorFactory, IOptions<EmaStrategyOptions> emaOptions)
        {
            var emaIndicatorFactory1 = emaIndicatorFactory;
            var emaOptions1 = emaOptions.Value;
            _shortEmaIndicator = emaIndicatorFactory1.GetIndicator(emaOptions1.ShortWeight);
            _longEmaIndicator = emaIndicatorFactory1.GetIndicator(emaOptions1.LongWeight);
        }

        public async Task<TrendDirection> CheckTrendAsync(decimal price)
        {
            var trendDirection = TrendDirection.Short;
            if (_lastBuyPrice == 0)
            {
                _lastBuyPrice = price;
            }

            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(price);
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(price);
            if (shortEmaValue > longEmaValue)
            {
                trendDirection = TrendDirection.Long;
                if (price >= _lastBuyPrice * (decimal)1.01)
                {
                    trendDirection = TrendDirection.Short;
                }
            }

            if (trendDirection != _lastTrend)
            {
                _lastTrend = trendDirection;
                _lastBuyPrice = trendDirection == TrendDirection.Long ? price : _lastBuyPrice;
                return await Task.FromResult(trendDirection);
            }

            return await Task.FromResult(TrendDirection.None);
        }
    }
}
