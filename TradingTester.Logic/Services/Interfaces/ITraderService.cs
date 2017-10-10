using System.Threading.Tasks;
using TradingTester.Logic.Models;

namespace TradingTester.Logic.Services.Interfaces
{
    public interface ITraderService
    {
        int TradingCount { get; }
        Task CheckStrategyAsync(CandleModel candle);
    }
}