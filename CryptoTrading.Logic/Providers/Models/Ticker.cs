namespace CryptoTrading.Logic.Providers.Models
{
    public class Ticker
    {
        public decimal Last { get; set; }

        public decimal LowestAsk { get; set; }

        public decimal HighestBid { get; set; }

        public decimal PercentChange { get; set; }

        public decimal BaseVolume { get; set; }

        public decimal QuoteVolume { get; set; }
    }
}