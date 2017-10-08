using System.Threading.Tasks;
using TradingTester.Logic.Models;

namespace TradingTester.Logic.Strategies.Interfaces
{
    public interface IStrategy
    {
        Task<TrendDirection> CheckTrendAsync(decimal price);
    }
}