namespace TradingTester.Logic.Indicators.Interfaces
{
    public interface IIndicatorFactory
    {
        IIndicator GetIndicator(int weight);
    }
}