using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Utils;
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
            var endTime = end ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var endPointUrl = $"/v2/trades/{tradingPair}/hist?limit=20000&start={start}&end={endTime}";
            var trades = await GetTradesAsync(endPointUrl);
            return CandleBatcher.MergeCandleDtos(trades.ToList(), start, endTime);
        }

        private async Task<IEnumerable<PoloniexTrade>> GetTradesAsync(string endPointUrl)
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
                    return JsonConvert.DeserializeObject<IEnumerable<PoloniexTrade>>(resultResponseContent);
                }
            }
        }
    }
}
