using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Strategies.Interfaces;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class EmaStrategy : IStrategy
    {
        private TrendDirection _lastTrend = TrendDirection.Short;
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private int _persistenceBuyCount = 1;
        private int _persistenceSellCount = 1;

        public EmaStrategy(IIndicatorFactory emaIndicatorFactory, IOptions<EmaStrategyOptions> emaOptions)
        {
            _shortEmaIndicator = emaIndicatorFactory.GetEmaIndicator(emaOptions.Value.ShortWeight);
            _longEmaIndicator = emaIndicatorFactory.GetEmaIndicator(emaOptions.Value.LongWeight);
        }

        public int CandleSize => 1;

        public async Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(previousCandles, currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(previousCandles, currentCandle).IndicatorValue;
            if (_lastTrend == TrendDirection.Short)
            {
                if (shortEmaValue > longEmaValue)
                {
                    if (_persistenceBuyCount > 2)
                    {
                        _lastTrend = TrendDirection.Long;
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
            else if (_lastTrend == TrendDirection.Long)
            {
                if (shortEmaValue < longEmaValue)
                {
                    if (_persistenceSellCount > 5)
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
                    return await Task.FromResult(TrendDirection.None);
                }
            }

            return await Task.FromResult(_lastTrend);
        }
    }
}
