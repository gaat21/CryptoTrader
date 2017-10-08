namespace TradingTester.Logic.Indicators.Interfaces
{
    public interface IIndicator
    {
        decimal GetIndicatorValue(decimal price);
    }
}
