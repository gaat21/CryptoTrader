using System.Collections.Generic;
using System.Threading.Tasks;
using TradingTester.Logic.Models;

namespace TradingTester.Logic.Providers.Interfaces
{
    public interface IExchangeProvider
    {
        Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, long since, int intervalInMin);
    }
}
