using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Strategies.Interfaces;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class CustomStrategy : IStrategy
    {
        private readonly IUserBalanceService _userBalanceService;
        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;

        public CustomStrategy(IIndicatorFactory indicatorFactory, IOptions<EmaStrategyOptions> emaOptions, IUserBalanceService userBalanceService)
        {
            _userBalanceService = userBalanceService;
            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(emaOptions.Value.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(emaOptions.Value.LongWeight);
        }

        public int CandleSize => 5;

        public async Task<TrendDirection> CheckTrendAsync(string tradingPair, List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var price = currentCandle.ClosePrice;
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;

            var emaTrend = shortEmaValue > longEmaValue ? TrendDirection.Long : TrendDirection.Short;
            //Console.WriteLine($"Short EMA value: {shortEmaValue}; Long EMA value: {longEmaValue}; EMA Trend: {emaTrend}; Candlesticks: {candleSticksValue}");
            if (_lastTrend == TrendDirection.Short)
            {
                if (emaTrend == TrendDirection.Long)
                {
                    _lastTrend = TrendDirection.Long;
                    _lastBuyPrice = price;
                }
                else
                {
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if(_lastTrend == TrendDirection.Long)
            {
                if (price >= _lastBuyPrice * (decimal) 1.01 
                    || price < _lastBuyPrice * (decimal) 0.9975)
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
