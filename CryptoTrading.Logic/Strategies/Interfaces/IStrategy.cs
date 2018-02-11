using System.Threading.Tasks;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Strategies.Interfaces
{
    public interface IStrategy
    {
        int DelayInCandlePeriod { get; }

        Task<TrendDirection> CheckTrendAsync(string tradingPair, CandleModel currentCandle);
    }
}