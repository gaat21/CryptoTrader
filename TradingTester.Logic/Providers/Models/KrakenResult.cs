using System.Collections.Generic;

namespace TradingTester.Logic.Providers.Models
{
    public class KrakenResult
    {
        public long Last { get; set; }

        public Dictionary<string, List<KrakenOhlc>> Candles { get; set; }
    }
}
