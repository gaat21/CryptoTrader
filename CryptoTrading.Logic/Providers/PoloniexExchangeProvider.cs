using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Utils;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace CryptoTrading.Logic.Providers
{
    public class PoloniexExchangeProvider : HttpBaseProvider, IExchangeProvider
    {
        private readonly PoloniexOptions _poloniexOptions;

        public PoloniexExchangeProvider(IOptions<PoloniexOptions> poloniexOptions) : base(poloniexOptions.Value.ApiUrl)
        {
            _poloniexOptions = poloniexOptions.Value;
        }

        public async Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, CandlePeriod candlePeriod, long start, long? end = null)
        {
            if (candlePeriod == CandlePeriod.OneMinute)
            {
                var endTime = end ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var endPointUrl = $"/public?command=returnTradeHistory&currencyPair={tradingPair}&start={start}&end={endTime}";
                var trades = await GetTradesAsync(endPointUrl);
                return CandleBatcher.MergeCandleDtos(trades.ToList(), start, endTime);
            }
            else
            {
                var endPointUrl = $"/public?command=returnChartData&currencyPair={tradingPair}&start={start}&end=9999999999&period=300";
                return await GetCandlesAsync(endPointUrl);
            }
        }

        private async Task<IEnumerable<CandleModel>> GetCandlesAsync(string endPointUrl)
        {
            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);

                using (var response = await client.GetAsync(endPointUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var resultResponseContent = await response.Content.ReadAsStringAsync();
                    var poloniexCandleList = JsonConvert.DeserializeObject<IEnumerable<PoloniexCandle>>(resultResponseContent);

                    return Mapper.Map<IEnumerable<CandleModel>>(poloniexCandleList);
                }
            }
        }

        private async Task<IEnumerable<PoloniexTrade>> GetTradesAsync(string endPointUrl)
        {
            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);

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
