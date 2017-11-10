using CryptoTrading.Logic.Indicators.Interfaces;

namespace CryptoTrading.Logic.Indicators
{
    public class EmaIndicatorFactory : IIndicatorFactory
    {
        public IIndicator GetIndicator(int weight)
        {
            return new EmaIndicator(weight);
        }
    }
}
