using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;

namespace CryptoTrading.Logic.Providers.Interfaces
{
    public interface IExchangeProvider
    {
        Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, CandlePeriod candlePeriod, long start, long? end);

        Task<long> CreateOrderAsync(TradeType tradeType, string tradingPair, decimal rate, decimal amount);

        Task<bool> CancelOrderAsync(string tradingPair, long orderNumber);

        Task<IEnumerable<OrderDetail>> GetOrderAsync(string tradingPair, long orderNumber);

        Task<Ticker> GetTicker(string tradingPair);
    }
}
