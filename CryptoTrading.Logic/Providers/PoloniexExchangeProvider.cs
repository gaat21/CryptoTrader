using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
        private const string PrivateEndpointPath = "/tradingApi";

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

        public async Task<OrderResult> CreateOrder(string tradingPair, decimal rate, decimal amount)
        {
            var formParameters = $"currencyPair={tradingPair}&rate={rate}&amount={amount}";
            var hmacHash = new HMACSHA512(Encoding.UTF8.GetBytes(_poloniexOptions.ApiSecret));
            var signedParametersByteArray = hmacHash.ComputeHash(Encoding.UTF8.GetBytes(formParameters));

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);
                client.DefaultRequestHeaders.Add("Sign", BitConverter.ToString(signedParametersByteArray).Replace("-", ""));

                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("currencyPair", tradingPair),
                    new KeyValuePair<string, string>("rate", rate.ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>("amount", amount.ToString(CultureInfo.InvariantCulture))
                });
                
                using (var response = await client.PostAsync(PrivateEndpointPath, requestBody))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var resultResponseContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PoloniexOrderResult>(resultResponseContent);
                }
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
