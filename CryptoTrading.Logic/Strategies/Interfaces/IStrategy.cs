using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Strategies.Interfaces
{
    public interface IStrategy
    {
        int CandleSize { get; }

        Task<TrendDirection> CheckTrendAsync(string tradingPair, List<CandleModel> previousCandles, CandleModel currentCandle);
    }
}