using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        private int _tradingCount;

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
            var lastSince = DateTimeOffset.UtcNow.AddMinutes(-1 * _strategy.CandleSize).ToUnixTimeSeconds();
            while (!cancellationToken.IsCancellationRequested)
            {
                var candles = await _exchangeProvider.GetCandlesAsync(tradingPair, lastSince, candlePeriod);
                var candlesList = candles.ToList();
                if (candlesList.ToList().Count != _strategy.CandleSize)
                {
                    throw new Exception("Candle size doesn't equal with getting candles");
                }
                var prevCandles = candlesList.GetRange(0, candlesList.Count - 1);
                var currentCandle = candlesList.Last();
                var trendDirection = await _strategy.CheckTrendAsync(prevCandles, currentCandle);
                
                if (trendDirection == TrendDirection.Long)
                {
                    await BuyAsync(currentCandle);
                }
                else if (trendDirection == TrendDirection.Short)
                {
                    await SellAsync(currentCandle);
                }

                await Task.Delay(DelayInMilliseconds, cancellationToken);
            }
        }

        public Task BuyAsync(CandleModel candle)
        {
            _userBalanceService.SetBuyPrice(candle.ClosePrice);
            Console.WriteLine($"Buy crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");

            return Task.FromResult(0);
        }

        public Task SellAsync(CandleModel candle)
        {
            Console.WriteLine($"Sell crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");
            _tradingCount++;
            Console.WriteLine($"Profit: ${_userBalanceService.GetProfit(candle.ClosePrice)}");

            return Task.FromResult(0);
        }
    }
}
