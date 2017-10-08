using TradingTester.Logic.Indicators.Interfaces;

namespace TradingTester.Logic.Indicators
{
    public class EmaIndicatorFactory : IIndicatorFactory
    {
        public IIndicator GetIndicator(int weight)
        {
            return new EmaIndicator(weight);
        }
    }
}
