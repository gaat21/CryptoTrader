namespace CryptoTrading.Logic.Services.Interfaces
{
    public interface IUserBalanceService
    {
        decimal? LastBuyPrice { get; }
        void SetBuyPrice(decimal price);
        decimal GetProfit(decimal sellPrice);
        decimal LastPrice { get; set; }
        decimal FirstPrice { get; set; }
        decimal TotalProfit { get; }
        decimal TotalNormalProfit { get; }
        decimal TotalProfitPercentage { get; }
        decimal TotalNormalProfitPercentage { get; }
    }
}