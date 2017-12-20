using CryptoTrading.Logic.Services.Models;

namespace CryptoTrading.Logic.Services.Interfaces
{
    public interface IUserBalanceService
    {
        decimal Rate { get; set; }
        void SetBuyPrice(decimal price);
        ProfitModel GetProfit(decimal sellPrice);
        ProfitModel GetProfit(decimal? profit);
        string TradingSummary();
        decimal LastPrice { get; set; }
        decimal FirstPrice { get; set; }
        int TradingCount { get; set; }
        bool HasOpenOrder { get; set; }
        long OpenOrderNumber { get; set; }
    }
}