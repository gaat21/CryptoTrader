using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Strategies.Interfaces;

namespace CryptoTrading.Logic.Strategies
{
    public class CandleSticksStrategy : IStrategy
    {
        private TrendDirection _lastTrend = TrendDirection.Short;
        private readonly IIndicator _candleSticksIndicator;
        private decimal _lastBuyPrice;
        public int DelayInCandlePeriod => 1;

        public CandleSticksStrategy(IIndicator candleSticksIndicator)
        {
            _candleSticksIndicator = candleSticksIndicator;
        }

        public Task<TrendDirection> CheckTrendAsync(string tradingPair, CandleModel currentCandle)
        {
            var candleSticksValue = _candleSticksIndicator.GetIndicatorValue(currentCandle);
            if (_lastTrend == TrendDirection.Short)
            {
                if (candleSticksValue.CandleFormat == CandleFormat.BullishMarubozu)
                {
                    _lastTrend = TrendDirection.Long;
                    return Task.FromResult(_lastTrend);
                }
            }
            if (_lastTrend == TrendDirection.Long)
            {
                if (currentCandle.ClosePrice >= _lastBuyPrice * (decimal)1.01
                    || candleSticksValue.CandleFormat == CandleFormat.BearishMarubozu)
                {
                    _lastTrend = TrendDirection.Short;
                }
                else
                {
                    return Task.FromResult(TrendDirection.None);
                }
            }

            return Task.FromResult(TrendDirection.None);
        }
    }
}
