using System.Collections.Generic;
using System.Threading.Tasks;
using TradingTester.Logic.Models;
using TradingTester.Logic.Providers.Models;

namespace TradingTester.Logic.Providers.Interfaces
{
    public interface IExchangeProvider
    {
        Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, long start, CandlePeriod candlePeriod);
    }
}
