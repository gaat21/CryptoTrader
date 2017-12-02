namespace CryptoTrading.Logic.Providers.Models
{
    public class PoloniexCandle : BaseCandle
    {
        public decimal WeightedAverage { get; set; }
        public decimal QuoteVolume { get; set; }
    }
}