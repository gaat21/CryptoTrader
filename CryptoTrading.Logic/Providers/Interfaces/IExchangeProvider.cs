using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;

namespace CryptoTrading.Logic.Providers.Interfaces
{
    public interface IExchangeProvider
    {
        Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, long start, CandlePeriod candlePeriod);
    }
}
