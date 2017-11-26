using CryptoTrading.Logic.Services.Interfaces;

namespace CryptoTrading.Logic.Services
{
    public class UserBalanceService : IUserBalanceService
    {
        private decimal _buyPrice;
        private decimal _profit;
        public decimal? LastBuyPrice { get; private set; }
        public decimal LastPrice { get; set; }
        public decimal FirstPrice { get; set; }

        public decimal TotalProfit => _profit;

        public decimal TotalNormalProfit => LastPrice - FirstPrice;

        public decimal TotalProfitPercentage
        {
            get
            {
                if (LastBuyPrice != null)
                {
                    return _profit / LastBuyPrice.Value * 100;
                }
                return 0;
            }
        }

        public decimal TotalNormalProfitPercentage => TotalNormalProfit / FirstPrice * 100;

        public void SetBuyPrice(decimal price)
        {
            if (!LastBuyPrice.HasValue)
            {
                LastBuyPrice = price;
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