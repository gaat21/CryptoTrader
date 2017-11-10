using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;

namespace CryptoTrading.Logic.Repositories.Interfaces
{
    public interface IImportRepository
    {
        Task<IEnumerable<CandleModel>> ImportCandlesAsync(string tradingPair, int intervalInHour, CandlePeriod candlePeriod);
    }
}
