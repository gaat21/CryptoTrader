using System;
using System.Collections.Generic;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Services.Models
{
    public class CandlePeriodModel
    {
        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public long ScanId { get; set; }

        public IEnumerable<CandleModel> Candles { get; set; }
    }
}