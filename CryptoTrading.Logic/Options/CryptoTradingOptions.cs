namespace CryptoTrading.Logic.Options
{
    public class CryptoTradingOptions
    {
        public decimal AmountInUsdt { get; set; }

        public bool EnableRealtimeTrading { get; set; }

        public decimal TradingFee { get; set; }

        public string EmailSubject { get; set; }
    }
}
