using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Strategies.Interfaces;
using CryptoTrading.Logic.Utils;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class IchimokuCloudStrategy : IStrategy
    {
        private readonly IIndicator _ichimokuCloudIndicator;

        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private readonly IIndicator _emaIndicator;
        private readonly IIndicator _rsiIndicator;

        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private int _candleCount = 1;
        private decimal _lastMacd;
        private FixedSizedQueue<decimal> _last5Macd;

        public int CandleSize { get; } = 1;

        public IchimokuCloudStrategy(IOptions<MacdStrategyOptions> options, IIndicatorFactory indicatorFactory)
        {
            _ichimokuCloudIndicator = indicatorFactory.GetIchimokuCloud();
            _emaIndicator = indicatorFactory.GetEmaIndicator(21);
            _rsiIndicator = indicatorFactory.GetRsiIndicator(7);

            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.LongWeight);

            _last5Macd = new FixedSizedQueue<decimal>(5);
        }

        public async Task<TrendDirection> CheckTrendAsync(string tradingPair, List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var macdValue = Math.Round(shortEmaValue - longEmaValue, 4);

            var rsiValue = _rsiIndicator.GetIndicatorValue(currentCandle).IndicatorValue;

            var ichimokuCloudValue = _ichimokuCloudIndicator.GetIndicatorValue(currentCandle);
            var emaValue = _emaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;

            Console.WriteLine($"DateTs: {currentCandle.StartDateTime:s}; " +
                              $"SSA: {ichimokuCloudValue.IchimokuCloud?.SenkouSpanAValue}; " +
                              $"SSB: {ichimokuCloudValue.IchimokuCloud?.SenkouSpanBValue}; " +
                              $"KS: {ichimokuCloudValue.IchimokuCloud?.KijunSenValue}; " +
                              $"EMA: {emaValue}; " +
                              $"MACD: {macdValue}; " +
                              $"RSI: {rsiValue}; " +
                              $"Close price: {currentCandle.ClosePrice};");

            // wait 1 hour
            if (_candleCount <= 60)
            {
                _candleCount++;
                _last5Macd.Enqueue(macdValue);
                return await Task.FromResult(TrendDirection.None);
            }

            var ssa = ichimokuCloudValue.IchimokuCloud?.SenkouSpanAValue;
            var ssb = ichimokuCloudValue.IchimokuCloud?.SenkouSpanBValue;
            if (_lastTrend == TrendDirection.Short)
            {
                if (currentCandle.ClosePrice > ssa + 3
                    && currentCandle.ClosePrice > ssb + 3
                    && _last5Macd.GetItems().All(a => a < 0)
                    && _last5Macd.MinPrice() < (decimal) -1.0
                    && macdValue > 0 && macdValue <= (decimal)1.0
                    && (rsiValue < 60 && rsiValue > 40)
                    && ssb != ichimokuCloudValue.IchimokuCloud?.KijunSenValue)
                {
                    _lastMacd = macdValue;
                    _last5Macd.Enqueue(macdValue);
                    _lastTrend = TrendDirection.Long;
                    _lastBuyPrice = currentCandle.ClosePrice;
                }
                else
                {
                    _last5Macd.Enqueue(macdValue);
                    _lastMacd = macdValue;
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if (_lastTrend == TrendDirection.Long)
            {
                var stopPercentage = (decimal) 0.98;
                var profitPercentage = (decimal) 1.03;
                if (_lastMacd > macdValue
                    && (currentCandle.ClosePrice < _lastBuyPrice * stopPercentage
                    || currentCandle.ClosePrice > _lastBuyPrice * profitPercentage))
                {
                    _last5Macd.Enqueue(macdValue);
                    _lastMacd = macdValue;
                    _lastTrend = TrendDirection.Short;
                }
                else
                {
                    _last5Macd.Enqueue(macdValue);
                    _lastMacd = macdValue;
                    return await Task.FromResult(TrendDirection.None);
                }
            }

            return await Task.FromResult(_lastTrend);
        }
    }
}
