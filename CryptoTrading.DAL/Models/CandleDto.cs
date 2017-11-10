using System;
using System.ComponentModel.DataAnnotations;

namespace CryptoTrading.DAL.Models
{
    public class CandleDto
    {
        [Key]
        public long Id { get; set; }

        public long ScanId { get; set; }
        public DateTime StartDateTime { get; set; }      
        public decimal HighPrice { get; set; }
        public decimal OpenPrice { get; set; }
        public decimal LowPrice { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal Volume { get; set; }
        public decimal VolumeWeightedPrice { get; set; }
        public string TradingPair { get; set; }
        public int TradingCount { get; set; }

    }

}
