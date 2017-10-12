using System;
using System.Threading.Tasks;
using TradingTester.Logic.Models;
using TradingTester.Logic.Services.Interfaces;
using TradingTester.Logic.Strategies.Interfaces;

namespace TradingTester.Logic.Services
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

        public async Task CheckStrategyAsync(CandleModel candle)
        {
            var trendDirection = await _strategy.CheckTrendAsync(candle.ClosePrice);
            if (trendDirection == TrendDirection.None)
            {
                //Console.WriteLine($"Nothing. Price: ${candle.ClosePrice}");
                return;
            }

            if (trendDirection == TrendDirection.Long)
            {
                BuyCryptoCurrency(candle);
                return;
            }
            SellCryptoCurrency(candle);
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
