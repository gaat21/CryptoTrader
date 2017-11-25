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
    public class CustomStrategy : IStrategy
    {
        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private int _persistenceBuyCount;
        private int _persistenceSellCount;

        public CustomStrategy(IIndicatorFactory emaIndicatorFactory, IOptions<EmaStrategyOptions> emaOptions)
        {
            _shortEmaIndicator = emaIndicatorFactory.GetEmaIndicator(emaOptions.Value.ShortWeight);
            _longEmaIndicator = emaIndicatorFactory.GetEmaIndicator(emaOptions.Value.LongWeight);
        }

        public int CandleSize => 1;

        public async Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var price = currentCandle.ClosePrice;
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(previousCandles, currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(previousCandles, currentCandle).IndicatorValue;

            //var emaTrend = shortEmaValue > longEmaValue ? TrendDirection.Long : TrendDirection.Short;
            //Console.WriteLine($"Short EMA value: {shortEmaValue}; Long EMA value: {longEmaValue}; EMA Trend: {emaTrend}");
            if (_lastTrend == TrendDirection.Short)
            {
                if (shortEmaValue > longEmaValue)
                {
                    if (_persistenceBuyCount > 4)
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
                if (price >= _lastBuyPrice * (decimal) 1.01 || price < _lastBuyPrice * (decimal) 0.997)
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
