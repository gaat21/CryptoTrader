using TradingTester.Logic.Services.Interfaces;

namespace TradingTester.Logic.Services
{
    public class UserBalanceService : IUserBalanceService
    {
        private decimal _buyPrice;
        private decimal _profit;

        public decimal TotalProfit => _profit;

        public void SetBuyPrice(decimal price)
        {
            _buyPrice = price;
        }

        public decimal GetProfit(decimal sellPrice)
        {
            decimal currentProfit = sellPrice - _buyPrice;
            _profit += currentProfit;
            return sellPrice - _buyPrice;
        }
    }
}