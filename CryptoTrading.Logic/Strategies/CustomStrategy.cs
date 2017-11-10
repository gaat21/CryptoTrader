using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Strategies.Interfaces;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class CustomStrategy : IStrategy
    {
        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private int _persistenceBuyCount;

        public CustomStrategy(IIndicatorFactory emaIndicatorFactory, IOptions<EmaStrategyOptions> emaOptions)
        {
            var emaIndicatorFactory1 = emaIndicatorFactory;
            var emaOptions1 = emaOptions.Value;
            _shortEmaIndicator = emaIndicatorFactory1.GetIndicator(emaOptions1.ShortWeight);
            _longEmaIndicator = emaIndicatorFactory1.GetIndicator(emaOptions1.LongWeight);
        }

        public int CandleSize => 1;

        public async Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var price = currentCandle.ClosePrice;
            if (_lastTrend == TrendDirection.Short)
            {
                var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(previousCandles, currentCandle).IndicatorValue;
                var longEmaValue = _longEmaIndicator.GetIndicatorValue(previousCandles, currentCandle).IndicatorValue;
                if (shortEmaValue > longEmaValue)
                {
                    if (_persistenceBuyCount > 2)
                    {
                        _lastTrend = TrendDirection.Long;
                        _lastBuyPrice = price;
                    }
                    else
                    {
                        _persistenceBuyCount++;
                        return await Task.FromResult(TrendDirection.None);
                    }
                }
                else
                {
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if(_lastTrend == TrendDirection.Long)
            {
                if (price >= _lastBuyPrice * (decimal) 1.03 || price < _lastBuyPrice * (decimal) 0.9)
                {
                    _lastTrend = TrendDirection.Short;
                }
                else
                {
                    return await Task.FromResult(TrendDirection.None);
                }
            }

            return await Task.FromResult(_lastTrend);
        }
    }
}
