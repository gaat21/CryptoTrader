using System;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Services.Models;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Services
{
    public class UserBalanceService : IUserBalanceService
    {
        private decimal _profit;
        private readonly decimal _defaultAmount;

        public UserBalanceService(IOptions<CryptoTradingOptions> cryptoTradingOptions)
        {
            _defaultAmount = cryptoTradingOptions.Value.AmountInUsdt;
        }

        ProfitModel IUserBalanceService.GetProfit(decimal sellPrice)
        {
            var sellProfit = sellPrice * Rate - _defaultAmount;
            _profit += sellProfit;
            LastPrice = sellPrice;
            return GetProfit(sellProfit);
        }

        public ProfitModel GetProfit(decimal? profit = null)
        {
            var firstRate = Math.Round(_defaultAmount / FirstPrice, 8);
            var normalProfit = Math.Round(LastPrice * firstRate - _defaultAmount, 8);
            return new ProfitModel
            {
                Profit = profit ?? 0,
                TotalProfit = _profit,
                TotalProfitPercentage = Math.Round(_profit / _defaultAmount * 100, 8),
                TotalNormalProfit = normalProfit,
                TotalNormalProfitPercentage = Math.Round(normalProfit / _defaultAmount * 100, 8)
            };
        }

        public string TradingSummary()
        {
            var profit = GetProfit();

            return $"Trading count: {TradingCount}\n" +
                   "\n" +
                   $"Total profit: ${profit.TotalProfit}\n" +
                   $"Total profit %: {decimal.Round(profit.TotalProfitPercentage, 2)}%\n" +
                   "\n" +
                   $"Total normal profit: ${ profit.TotalNormalProfit}\n" +
                   $"Total normal profit %: {decimal.Round(profit.TotalNormalProfitPercentage, 2)}%\n" +
                   "\n";
        }

        public decimal FirstPrice { get; set; }
        public decimal LastPrice { get; set; }

        public int TradingCount { get; set; }

        public decimal Rate { get; set; }

        public void SetBuyPrice(decimal buyPrice)
        {
            Rate = Math.Round(_defaultAmount / buyPrice, 8);
        }

        public bool HasOpenOrder { get; set; } = false;

        public long OpenOrderNumber { get; set; }
    }
}