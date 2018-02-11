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
        private readonly FixedSizedQueue<decimal> _last5Macd;
        private int _buySignCount = 1;
        private int _delayCount = 1;

        public int DelayInCandlePeriod => 180;

        public IchimokuCloudStrategy(IOptions<MacdStrategyOptions> options, IIndicatorFactory indicatorFactory)
        {
            _ichimokuCloudIndicator = indicatorFactory.GetIchimokuCloud();
            _emaIndicator = indicatorFactory.GetEmaIndicator(21);
            _rsiIndicator = indicatorFactory.GetRsiIndicator(7);

            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(options.Value.LongWeight);

            _last5Macd = new FixedSizedQueue<decimal>(5);
        }

        public async Task<TrendDirection> CheckTrendAsync(string tradingPair, CandleModel currentCandle)
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
                              $"TS: {ichimokuCloudValue.IchimokuCloud?.TenkanSenValue}; " +
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
            var ts = ichimokuCloudValue.IchimokuCloud?.TenkanSenValue;
            var ks = ichimokuCloudValue.IchimokuCloud?.KijunSenValue;

            //var ssaIncrease = false;
            //var ssbIncrease = false;
            //var ssbGreaterThenSsa = false;
            //if (ichimokuCloudValue.IchimokuCloud != null)
            //{
            //    ssaIncrease = ichimokuCloudValue.IchimokuCloud.SsaFuture.Last() > ichimokuCloudValue.IchimokuCloud.SsaFuture.First();
            //    ssbIncrease = ichimokuCloudValue.IchimokuCloud.SsbFuture.Last() > ichimokuCloudValue.IchimokuCloud.SsbFuture.First();
            //    ssbGreaterThenSsa = ichimokuCloudValue.IchimokuCloud.SsbFuture.Last() > ichimokuCloudValue.IchimokuCloud.SsaFuture.Last();
            //}
            if (_lastTrend == TrendDirection.Short)
            {
                if (currentCandle.ClosePrice > ssa
                    && currentCandle.ClosePrice > ssb
                    && currentCandle.CandleType == CandleType.Green
                    && ts > ks
                    && currentCandle.ClosePrice > ts
                    && currentCandle.ClosePrice > ks
                    )
                {
                    _lastMacd = macdValue;
                    _last5Macd.Enqueue(macdValue);
                    //Console.WriteLine($"Buy sign count: {_buySignCount}");
                    //if (_buySignCount < 2)
                    //{
                    //    _buySignCount++;
                    //    return await Task.FromResult(TrendDirection.None);
                    //}

                    _buySignCount = 1;
                    _lastTrend = TrendDirection.Long;
                    _lastBuyPrice = currentCandle.ClosePrice;
                }
                else
                {
                    _buySignCount = 1;
                    _last5Macd.Enqueue(macdValue);
                    _lastMacd = macdValue;
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if (_lastTrend == TrendDirection.Long)
            {
                if (_delayCount < 10)
                {
                    Console.WriteLine($"DelayCount: {_delayCount}");
                    _delayCount++;
                    _last5Macd.Enqueue(macdValue);
                    _lastMacd = macdValue;
                    return await Task.FromResult(TrendDirection.None);
                }

                //var crossOver = ichimokuCloudValue.IchimokuCloud?.SsaCrossoverSsb ?? false;
                var stopPercentage = (decimal)0.98;
                //var minProfitPercentage = (decimal) 1.015;
                //var maxProfitPercentage = (decimal)1.04;
                if (currentCandle.ClosePrice < _lastBuyPrice * stopPercentage
                    /*|| currentCandle.ClosePrice > _lastBuyPrice * maxProfitPercentage*/)
                {
                    _last5Macd.Enqueue(macdValue);
                    _lastMacd = macdValue;
                    _delayCount = 1;
                    _lastTrend = TrendDirection.Short;
                }
                else if (
                        currentCandle.ClosePrice < ts 
                        && currentCandle.ClosePrice < ks
                        && currentCandle.ClosePrice < ssa 
                         || currentCandle.ClosePrice < ssb
                        //&& rsiValue >= 80
                        //&& macdValue > 0
                        //&& currentCandle.ClosePrice > _lastBuyPrice * minProfitPercentage
                    )
                {
                    _last5Macd.Enqueue(macdValue);
                    _lastMacd = macdValue;
                    _delayCount = 1;
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
