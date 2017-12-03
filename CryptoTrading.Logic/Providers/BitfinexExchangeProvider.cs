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
                            OpenPrice = (decimal)result[1],
                            ClosePrice = (decimal)result[2],
                            HighPrice = (decimal)result[3],
                            LowPrice = (decimal)result[4],
                            Volume = (long)result[5]
                        });

                    return candles;
                }
            }
        }
    }
}
