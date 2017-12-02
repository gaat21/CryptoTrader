using System;

namespace CryptoTrading.Logic.Providers.Models
{
    public class BaseCandle
    {
        public DateTime StartDateTime => DateTimeOffset.FromUnixTimeSeconds(Date).DateTime;
        public long Date { get; set; }
        public decimal High { get; set; }
        public decimal Open { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}