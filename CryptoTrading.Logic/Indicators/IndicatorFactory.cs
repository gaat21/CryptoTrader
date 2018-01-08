using CryptoTrading.Logic.Indicators.Interfaces;

namespace CryptoTrading.Logic.Indicators
{
    public class IndicatorFactory : IIndicatorFactory
    {
        public IIndicator GetEmaIndicator(int weight)
        {
            return new EmaIndicator(weight);
        }

        public IIndicator GetRsiIndicator(int weight)
        {
            return new RsiIndicator(weight);
        }

        public IIndicator GetMfiIndicator(int weight)
        {
            return new MfiIndicator(weight);
        }

        public IIndicator GetTdiIndicator(int period)
        {
            return new TdiIndicator(period);
        }
    }
}
