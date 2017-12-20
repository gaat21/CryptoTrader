using System.Collections.Generic;

namespace CryptoTrading.Logic.Providers.Models
{
    public class OrderBook
    {
        public List<decimal> SellOrders { get; set; }

        public List<decimal> BuyOrders { get; set; }

        public bool IsFrozen { get; set; }

        public long SequenceNumber { get; set; }
    }
}