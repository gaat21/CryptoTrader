using System.Collections.Generic;
using System.Threading.Tasks;
using TradingTester.Logic.Models;

namespace TradingTester.Logic.Repositories.Interfaces
{
    public interface IImportRepository
    {
        Task<IEnumerable<CandleModel>> ImportCandlesAsync(string tradingPair, int intervalInHour, int candlePeriod);
    }
}
