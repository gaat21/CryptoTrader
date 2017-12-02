namespace CryptoTrading.Logic.Providers.Models
{    
    public class KrakenCandle : BaseCandle
    {
        public decimal VolumeWeightedPrice { get; set; }
        public int TradingCount { get; set; }
    }
}
