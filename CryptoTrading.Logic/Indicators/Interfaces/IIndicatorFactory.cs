namespace CryptoTrading.Logic.Indicators.Interfaces
{
    public interface IIndicatorFactory
    {
        IIndicator GetEmaIndicator(int weight);

        IIndicator GetRsiIndicator(int weight);

        IIndicator GetMfiIndicator(int weight);

        IIndicator GetTdiIndicator(int period);

        IIndicator GetIchimokuCloud();
    }
}