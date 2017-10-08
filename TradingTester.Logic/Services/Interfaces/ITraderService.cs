using System.Threading.Tasks;
using TradingTester.Logic.Models;

namespace TradingTester.Logic.Services.Interfaces
{
    public interface ITraderService
    {
        Task CheckStrategyAsync(CandleModel candle);
    }
}