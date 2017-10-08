using System.Collections.Generic;
using System.Threading.Tasks;

namespace TradingTester.Logic.Services.Interfaces
{
    public interface ICandleService
    {
        Task<IEnumerable<CandlePeriodModel>> GetAvailableCandlePeriodsAsync(string tradingPair);
    }
}