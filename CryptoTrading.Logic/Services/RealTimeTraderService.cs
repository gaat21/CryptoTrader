using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CryptoTrading.DAL.Models;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Repositories.Interfaces;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Strategies.Interfaces;

namespace CryptoTrading.Logic.Services
{
    public class RealTimeTraderService : ITraderService
    {
        private readonly IStrategy _strategy;
        private readonly IUserBalanceService _userBalanceService;
        private readonly IExchangeProvider _exchangeProvider;
        private readonly ICandleRepository _candleRepository;
        private const int DelayInMilliseconds = 60000;

        private bool _isSetFirstPrice;
        private int _tradingCount;

        private static TrendDirection _lastTrendDirection;

        public int TradingCount => _tradingCount;

        public RealTimeTraderService(IStrategy strategy,
                                     IUserBalanceService userBalanceService,
                                     IExchangeProvider exchangeProvider, 
                                     ICandleRepository candleRepository)
        {
            _strategy = strategy;
            _userBalanceService = userBalanceService;
            _exchangeProvider = exchangeProvider;
            _candleRepository = candleRepository;
        }

        public async Task CheckStrategyAsync(List<CandleModel> candles)
        {
            for (int i = 0; i < candles.Count; i++)
            {
                var startIndex = i - _strategy.CandleSize;
                var prevCandles = candles.GetRange(startIndex < 0 ? 0 : startIndex, _strategy.CandleSize);
                var currentCandle = candles[i];
                var trendDirection = await _strategy.CheckTrendAsync(prevCandles, currentCandle);

                if (trendDirection == TrendDirection.None)
                {
                    continue;
                }

                if (trendDirection == TrendDirection.Long)
                {
                    await BuyAsync(currentCandle);
                    continue;
                }
                await SellAsync(currentCandle);
            }
        }

        public async Task StartTradingAsync(string tradingPair, CandlePeriod candlePeriod, CancellationToken cancellationToken)
        {
            var lastSince = GetSinceUnixTime(candlePeriod);
            var lastScanId = _candleRepository.GetLatestScanId();
            CandleModel currentCandle = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                var startWatcher = new Stopwatch();
                startWatcher.Start();
                var candles = await _exchangeProvider.GetCandlesAsync(tradingPair, candlePeriod, lastSince, lastSince + (int)candlePeriod * 60);
                var candlesList = candles.ToList();
                if (candlesList.Count == _strategy.CandleSize)
                {
                    var prevCandles = candlesList.GetRange(0, candlesList.Count - 1);
                    currentCandle = candlesList.Last();
                    if (!_isSetFirstPrice)
                    {
                        _userBalanceService.FirstPrice = currentCandle.ClosePrice;
                        _isSetFirstPrice = true;
                    }
                    var trendDirection = await _strategy.CheckTrendAsync(prevCandles, currentCandle);

                    await _candleRepository.SaveCandleAsync(tradingPair, Mapper.Map<List<CandleDto>>(new List<CandleModel> {currentCandle}), lastScanId);

                    Console.WriteLine($"DateTs: {DateTimeOffset.FromUnixTimeSeconds(lastSince):s}; Trend: {trendDirection}; Close price: {currentCandle.ClosePrice}; Volumen: {currentCandle.Volume}; Elapsed time: {startWatcher.ElapsedMilliseconds} ms");
                    _lastTrendDirection = trendDirection;
                    if (trendDirection == TrendDirection.Long)
                    {
                        await BuyAsync(currentCandle);
                    }
                    else if (trendDirection == TrendDirection.Short)
                    {
                        await SellAsync(currentCandle);
                    }
                }
                else
                {
                    Console.WriteLine($"DateTs: {DateTimeOffset.FromUnixTimeSeconds(lastSince):s}; Trend: [NO TRADES]; Close price: [NO TRADES]; Volumen: [NO TRADES]; Elapsed time: {startWatcher.ElapsedMilliseconds} ms");
                }

                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay((int)(DelayInMilliseconds - startWatcher.ElapsedMilliseconds));
                lastSince = GetSinceUnixTime(candlePeriod);
                startWatcher.Stop();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _userBalanceService.LastPrice = currentCandle.ClosePrice;
                if (_lastTrendDirection == TrendDirection.Long)
                {
                    // ReSharper disable once MethodSupportsCancellation
                    SellAsync(currentCandle).Wait();
                }
            }
        }

        public Task BuyAsync(CandleModel candle)
        {
            if (candle != null)
            {
                _userBalanceService.SetBuyPrice(candle.ClosePrice);
                Console.WriteLine($"Buy crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");
            }

            return Task.FromResult(0);
        }

        public Task SellAsync(CandleModel candle)
        {
            if (candle != null)
            {
                Console.WriteLine($"Sell crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");
                _tradingCount++;
                Console.WriteLine($"Profit: ${_userBalanceService.GetProfit(candle.ClosePrice)}");
                Console.WriteLine();
                Console.WriteLine($"Trading count: {TradingCount}");
                Console.WriteLine($"Total profit: ${_userBalanceService.TotalProfit}");
                Console.WriteLine($"Total profit %: {decimal.Round(_userBalanceService.TotalProfitPercentage, 2)}%");
                Console.WriteLine();
                Console.WriteLine($"Total normal profit: ${_userBalanceService.TotalNormalProfit}");
                Console.WriteLine($"Total normal profit %: {decimal.Round(_userBalanceService.TotalNormalProfitPercentage, 2)}%");
            }

            return Task.FromResult(0);
        }

        private long GetSinceUnixTime(CandlePeriod candlePeriod)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var candlePeridInMinutes = (int) candlePeriod;
            var candleSizeInSeconds = candlePeridInMinutes * _strategy.CandleSize * 60;

            return utcNow.AddSeconds(-1 * (candleSizeInSeconds + utcNow.Second)).ToUnixTimeSeconds();
        }
    }
}
