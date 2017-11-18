namespace CryptoTrading.Logic.Indicators.Interfaces
{
    public interface IIndicatorFactory
    {
        IIndicator GetEmaIndicator(int weight);

        IIndicator GetRsiIndicator(int weight);
    }
}