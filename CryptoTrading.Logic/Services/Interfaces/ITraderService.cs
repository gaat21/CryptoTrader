using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Services.Interfaces
{
    public interface ITraderService
    {
        int TradingCount { get; }
        Task CheckStrategyAsync(List<CandleModel> candles);
    }
}