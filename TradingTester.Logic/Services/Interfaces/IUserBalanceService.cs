using System.Threading.Tasks;

namespace TradingTester.Logic.Services.Interfaces
{
    public interface IUserBalanceService
    {
        decimal TotalProfit { get; }
        void SetBuyPrice(decimal price);
        decimal GetProfit(decimal sellPrice);
    }
}