using TradingTester.Logic.Services.Interfaces;

namespace TradingTester.Logic.Services
{
    public class UserBalanceService : IUserBalanceService
    {
        private decimal _buyPrice;
        private decimal _profit;
        private decimal? _firstBuyPrice;

        public decimal TotalProfit => _profit;

        public decimal TotalPercentage => (_profit / _firstBuyPrice.Value) * 100;

        public void SetBuyPrice(decimal price)
        {
            if (!_firstBuyPrice.HasValue)
            {
                _firstBuyPrice = price;
            }
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