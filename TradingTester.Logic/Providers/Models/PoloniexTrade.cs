using System;
using TradingTester.Logic.Utils;

namespace TradingTester.Logic.Providers.Models
{
    public class PoloniexTrade
    {
        public DateTime Date { get; set; }

        public long DateTs => Helper.DateTimeToUnixTimestamp(Date);

        public TradeType Type { get; set; }
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
        public decimal Total { get; set; }
    }
}
