using System;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Services.Models;

namespace CryptoTrading.Logic.Services.Interfaces
{
    public interface IUserBalanceService
    {
        decimal Rate { get; set; }
        void SetBuyPrice(decimal price);
        ProfitModel GetProfit(decimal sellPrice, DateTime candleDateTime);
        ProfitModel GetProfit(decimal? profit);
        string TradingSummary();
        CandleModel LastPrice { get; set; }
        CandleModel FirstPrice { get; set; }
        int TradingCount { get; set; }
        bool HasOpenOrder { get; set; }
        long OpenOrderNumber { get; set; }
        bool EnableRealtimeTrading { get; }
    }
}