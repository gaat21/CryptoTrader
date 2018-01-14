using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CryptoTrading.Logic.Providers
{
    public class BitfinexExchangeProvider : HttpBaseProvider, IExchangeProvider
    {
        public BitfinexExchangeProvider(IOptions<BitfinexOptions> bitfinexOptions) : base(bitfinexOptions.Value.ApiUrl)
        {
        }

        public async Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, CandlePeriod candlePeriod, long start, long? end)
        {
            var endPointUrl = $"v2/candles/trade:{(int)candlePeriod}m:{tradingPair}/last";
            return await GetTradesAsync(endPointUrl);
        }

        public Task<long> CreateOrderAsync(TradeType tradeType, string tradingPair, decimal rate, decimal amount)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CancelOrderAsync(string tradingPair, long orderNumber)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OrderDetail>> GetOrderAsync(string tradingPair, long orderNumber)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetBalanceAsync()
        {
            throw new NotImplementedException();
        }

        public Task<OrderBook> GetOrderBook(string tradingPair, int depth)
        {
            throw new NotImplementedException();
        }

        public Task<Ticker> GetTicker(string tradingPair)
        {
            throw new NotImplementedException();
        }

        private async Task<IEnumerable<CandleModel>> GetTradesAsync(string endPointUrl)
        {
            using (var client = GetClient())
            {
                using (var response = await client.GetAsync(endPointUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var resultResponseContent = await response.Content.ReadAsStringAsync();
                    var candles = new List<CandleModel>();
                    if (resultResponseContent == "null")
                    {
                        return candles;
                    }
                    var result = JsonConvert.DeserializeObject<object[]>(resultResponseContent);
                    candles.Add(new CandleModel
                        {
                            StartDateTime = DateTimeOffset.FromUnixTimeSeconds((long)result[0] / 1000).DateTime,
                            OpenPrice = decimal.Parse(result[1].ToString()),
                            ClosePrice = decimal.Parse(result[2].ToString()),
                            HighPrice = decimal.Parse(result[3].ToString()),
                            LowPrice = decimal.Parse(result[4].ToString()),
                            Volume = decimal.Parse(result[5].ToString())
                        });

                    return candles;
                }
            }
        }
    }
}
