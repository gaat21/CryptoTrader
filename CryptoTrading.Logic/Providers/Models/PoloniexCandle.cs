namespace CryptoTrading.Logic.Providers.Models
{
    public class PoloniexCandle
    {
        public long Date { get; set; }
        public decimal High { get; set; }
        public decimal Open { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal WeightedAverage { get; set; }
        public decimal QuoteVolume { get; set; }
    }
}