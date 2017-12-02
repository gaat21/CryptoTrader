using System.Collections.Generic;

namespace CryptoTrading.Logic.Providers.Models
{
    public class KrakenResult
    {
        public long Last { get; set; }

        public Dictionary<string, List<KrakenCandle>> Candles { get; set; }
    }
}
