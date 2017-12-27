using System;
using CryptoTrading.Logic.Models;
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
            EnableRealtimeTrading = cryptoTradingOptions.Value.EnableRealtimeTrading;
        }

        ProfitModel IUserBalanceService.GetProfit(decimal sellPrice, DateTime candleDateTime)
        {
            var sellProfit = sellPrice * Rate - _defaultAmount;
            _profit += sellProfit;
            LastPrice = new CandleModel
            {
                ClosePrice = sellPrice,
                StartDateTime = candleDateTime
            };
            return GetProfit(sellProfit);
        }

        public ProfitModel GetProfit(decimal? profit = null)
        {
            var firstRate = Math.Round(_defaultAmount / FirstPrice.ClosePrice, 8);
            var normalProfit = Math.Round(LastPrice.ClosePrice * firstRate - _defaultAmount, 8);
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

            var totalDays = Math.Round((LastPrice.StartDateTime - FirstPrice.StartDateTime).TotalHours / 24, 4);
            var totalProfitPercantagePerDay = Math.Round(profit.TotalProfit / (decimal)totalDays, 4);
            return $"Trading count: {TradingCount}\n" +
                   "\n" +
                   $"Total profit: ${profit.TotalProfit}\n" +
                   $"Total profit %: {decimal.Round(profit.TotalProfitPercentage, 2)}%\n" +
                   "\n" +
                   $"Total normal profit: ${ profit.TotalNormalProfit}\n" +
                   $"Total normal profit %: {decimal.Round(profit.TotalNormalProfitPercentage, 2)}%\n" +
                   "\n" +
                   $"Total day(s): {totalDays}\n" +
                   $"Total profit % per day: {totalProfitPercantagePerDay}\n";
        }

        public CandleModel FirstPrice { get; set; }
        public CandleModel LastPrice { get; set; }

        public int TradingCount { get; set; }

        public decimal Rate { get; set; }

        public void SetBuyPrice(decimal buyPrice)
        {
            Rate = Math.Round(_defaultAmount / buyPrice, 8);
        }

        public bool HasOpenOrder { get; set; } = false;

        public long OpenOrderNumber { get; set; }

        public bool EnableRealtimeTrading { get; }
    }
}