using System;

namespace TradingTester.Logic.Models
{
    public class CandleModel
    {
        public DateTime StartDateTime { get; set; }
        public decimal HighPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
        public decimal VolumeWeightedPrice { get; set; }
        public int TradingCount { get; set; }
    }
}