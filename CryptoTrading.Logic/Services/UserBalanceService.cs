using CryptoTrading.Logic.Services.Interfaces;

namespace CryptoTrading.Logic.Services
{
    public class UserBalanceService : IUserBalanceService
    {
        private decimal _buyPrice;
        private decimal _profit;
        private decimal? _firstBuyPrice;
        public decimal LastPrice { get; set; }
        public decimal FirstPrice { get; set; }

        public decimal TotalProfit => _profit;

        public decimal TotalNormalProfit => LastPrice - FirstPrice;

        public decimal TotalProfitPercentage
        {
            get
            {
                if (_firstBuyPrice != null)
                {
                    return _profit / _firstBuyPrice.Value * 100;
                }
                return 0;
            }
        }

        public decimal TotalNormalProfitPercentage => TotalNormalProfit / FirstPrice * 100;

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