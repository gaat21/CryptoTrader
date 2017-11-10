using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
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
                    BuyCryptoCurrency(currentCandle);
                    continue;
                }
                SellCryptoCurrency(currentCandle);
            }
        }

        private void SellCryptoCurrency(CandleModel candle)
        {
            Console.WriteLine($"Sell crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");
            _tradingCount++;
            Console.WriteLine($"Profit: ${_userBalanceService.GetProfit(candle.ClosePrice)}");
        }

        private void BuyCryptoCurrency(CandleModel candle)
        {
            _userBalanceService.SetBuyPrice(candle.ClosePrice);
            Console.WriteLine($"Buy crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");
        }
    }
}
