using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CryptoTrading.Logic.Providers
{
    public class BinanceExchangeProvider : HttpBaseProvider, IExchangeProvider
    {
        public BinanceExchangeProvider(IOptions<BinanceOptions> binanceOptions) : base(binanceOptions.Value.ApiUrl)
        {
        }

        public async Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, CandlePeriod candlePeriod, long start, long? end)
        {
            var endTime = end ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var endPointUrl = $"/api/v1/klines?symbol={tradingPair}&interval={(int)candlePeriod}m&startTime={start * 1000}&endTime={(endTime * 1000) - 1}";
            
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
                    var result = JsonConvert.DeserializeObject<object[]>(resultResponseContent).ToList();
                    foreach (var item in result)
                    {
                        var arrayResult = JsonConvert.DeserializeObject<object[]>(item.ToString());
                        candles.Add(new CandleModel
                        {
                            StartDateTime = DateTimeOffset.FromUnixTimeSeconds((long)arrayResult[0] / 1000).DateTime,
                            OpenPrice = decimal.Parse(arrayResult[1].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture),
                            HighPrice = decimal.Parse(arrayResult[2].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture),
                            LowPrice = decimal.Parse(arrayResult[3].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture),
                            ClosePrice = decimal.Parse(arrayResult[4].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture),
                            Volume = decimal.Parse(arrayResult[5].ToString(), NumberStyles.Any, CultureInfo.InvariantCulture)
                        });
                    }
                    
                    return candles;
                }
            }
        }

        public Task<long> CreateOrderAsync(TradeType tradeType, string tradingPair, decimal rate, decimal amount)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CancelOrderAsync(long orderNumber)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OrderDetail>> GetOrderAsync(long orderNumber)
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
    }
}
