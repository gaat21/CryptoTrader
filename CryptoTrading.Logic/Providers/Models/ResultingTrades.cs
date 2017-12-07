using System;

namespace CryptoTrading.Logic.Providers.Models
{
    public class ResultingTrades
    {
        public decimal Amount { get; set; }

        public DateTime Date { get; set; }

        public decimal Rate { get; set; }

        public decimal Total { get; set; }

        public int TradeId { get; set; }

        public TradeType Type { get; set; }
    }
}