using System.Collections.Generic;
using System.Threading.Tasks;
using TradingTester.DAL.Models;

namespace TradingTester.Logic.Repositories.Interfaces
{
    public interface ICandleRepository
    {
        Task SaveCandleAsync(string tradingPair, List<CandleDto> candlesDto);

        Task<Dictionary<long, List<CandleDto>>> GetCandlesAsync(string tradingPair);
    }
}
