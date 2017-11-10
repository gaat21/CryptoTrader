using System.Collections.Generic;

namespace CryptoTrading.Logic.Providers.Models
{
    public class KrakenResponse<T>
    {
        public IEnumerable<string> Error { get; set; }

        public T Result { get; set; }
    }
}
