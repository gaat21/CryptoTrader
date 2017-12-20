using System.Collections.Generic;

namespace CryptoTrading.Logic.Providers.Models
{
    public class PoloniexOrderResult : OrderResult
    {
        public int OrderNumber { get; set; }

        public IList<PoloniexTrade> ResultingTrades { get; set; }
    }
}