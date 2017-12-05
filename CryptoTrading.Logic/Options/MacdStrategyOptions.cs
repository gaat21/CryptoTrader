namespace CryptoTrading.Logic.Options
{
    public class MacdStrategyOptions
    {
        public int LongWeight { get; set; }

        public int ShortWeight { get; set; }

        public int Signal { get; set; }

        public decimal BuyThreshold { get; set; }

        public decimal SellThreshold { get; set; }
    }
}
