using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Strategies.Interfaces;

namespace CryptoTrading.Logic.Services
{
    public class BacktestTraderService : ITraderService
    {
        private readonly IStrategy _strategy;
        private readonly IUserBalanceService _userBalanceService;
        private int _tradingCount;
        
        public BacktestTraderService(IStrategy strategy, IUserBalanceService userBalanceService)
        {
            _strategy = strategy;
            _userBalanceService = userBalanceService;
        }

        public int TradingCount => _tradingCount;

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
                    //Console.WriteLine($"Nothing. Price: ${candle.ClosePrice}");
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

        public Task StartTradingAsync(string tradingPair, CandlePeriod candlePeriod, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SellAsync(CandleModel candle)
        {
            Console.WriteLine($"Sell crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");
            _tradingCount++;
            Console.WriteLine($"Profit: ${_userBalanceService.GetProfit(candle.ClosePrice)}");

            return Task.FromResult(0);
        }

        public Task BuyAsync(CandleModel candle)
        {
            _userBalanceService.SetBuyPrice(candle.ClosePrice);
            Console.WriteLine($"Buy crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");

            return Task.FromResult(0);
        }
    }
}
