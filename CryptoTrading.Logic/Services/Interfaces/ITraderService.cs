using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;

namespace CryptoTrading.Logic.Services.Interfaces
{
    public interface ITraderService
    {
        int TradingCount { get; }
        Task CheckStrategyAsync(List<CandleModel> candles);
        Task StartTradingAsync(string tradingPair, CandlePeriod candlePeriod, CancellationToken cancellationToken);
        Task BuyAsync(CandleModel candle);
        Task SellAsync(CandleModel candle);
    }
}