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
using Newtonsoft.Json.Linq;

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

        private int GenerateNonce()
        {
            return (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
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
       
        public async Task<bool> CancelOrderAsync(long orderNumber)
        {
            var nonce = GenerateNonce();
            var formParameters = $"command=cancelOrder&nonce={nonce}&orderNumber={orderNumber}";

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);
                client.DefaultRequestHeaders.Add("Sign", SignPostBody(formParameters));

                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("command", "cancelOrder"),
                    new KeyValuePair<string, string>("nonce", nonce.ToString()),
                    new KeyValuePair<string, string>("orderNumber", orderNumber.ToString())
                });

                using (var response = await client.PostAsync(PrivateEndpointPath, requestBody))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var responseDefinition = new { Success = false };
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var deserializedResponse = JsonConvert.DeserializeAnonymousType(responseContent, responseDefinition);
                    return deserializedResponse.Success;
                }
            }
        }

        public async Task<IEnumerable<OrderDetail>> GetOrderAsync(long orderNumber)
        {
            var nonce = GenerateNonce();
            var formParameters = $"command=returnOrderTrades&nonce={nonce}&orderNumber={orderNumber}";

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);
                client.DefaultRequestHeaders.Add("Sign", SignPostBody(formParameters));

                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("command", "returnOrderTrades"),
                    new KeyValuePair<string, string>("nonce", nonce.ToString()),
                    new KeyValuePair<string, string>("orderNumber", orderNumber.ToString())
                });

                using (var response = await client.PostAsync(PrivateEndpointPath, requestBody))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }
                    
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent.ToLower().Contains("error"))
                    {
                        Console.WriteLine($"OrderNumber: {orderNumber}; Error message: {responseContent}");
                        return null;
                    }
                    return JsonConvert.DeserializeObject<IEnumerable<OrderDetail>>(responseContent);
                }
            }
        }

        public async Task<string> GetBalanceAsync()
        {
            var nonce = GenerateNonce();
            var formParameters = $"command=returnBalances&nonce={nonce}";

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);
                client.DefaultRequestHeaders.Add("Sign", SignPostBody(formParameters));

                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("command", "returnBalances"),
                    new KeyValuePair<string, string>("nonce", nonce.ToString())
                });

                using (var response = await client.PostAsync(PrivateEndpointPath, requestBody))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    return await response.Content.ReadAsStringAsync();
                }
            }
        }

        public async Task<OrderBook> GetOrderBook(string tradingPair, int depth = 1)
        {
            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);

                var endPointUrl = $"/public?command=returnOrderBook&currencyPair={tradingPair}&depth={depth}";
                using (var response = await client.GetAsync(endPointUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var resultResponseContent = await response.Content.ReadAsStringAsync();
                    var obj = (JObject)JsonConvert.DeserializeObject(resultResponseContent);
                    var isFrozen = int.Parse(obj["isFrozen"].Value<string>());
                    var seq = obj["seq"].Value<long>();

                    var orderBook = new OrderBook
                    {
                        IsFrozen = isFrozen != 0,
                        SequenceNumber = seq,
                        SellOrders = new List<decimal>(),
                        BuyOrders = new List<decimal>()
                    };

                    var asks = obj["asks"].Values().ToList();
                    var bids = obj["bids"].Values().ToList();
                    for (var i = 0; i < asks.Count; i+=2)
                    {
                        orderBook.BuyOrders.Add(bids.ElementAt(i).Value<decimal>());
                        orderBook.SellOrders.Add(asks.ElementAt(i).Value<decimal>());
                    }

                    return orderBook;
                }
            }
        }

        public async Task<Ticker> GetTicker(string tradingPair)
        {
            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);

                var endPointUrl = "/public?command=returnTicker";
                using (var response = await client.GetAsync(endPointUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var resultResponseContent = await response.Content.ReadAsStringAsync();
                    var obj = (JObject)JsonConvert.DeserializeObject(resultResponseContent);
                    var currencyPairContent = obj[tradingPair].ToString();
                    return JsonConvert.DeserializeObject<Ticker>(currencyPairContent);
                }
            }
        }

        public async Task<long> CreateOrderAsync(TradeType tradeType, string tradingPair, decimal rate, decimal amount)
        {
            var nonce = GenerateNonce();
            var formParameters = $"command={tradeType.ToString().ToLower()}" +
                                 $"&nonce={nonce}" +
                                 $"&currencyPair={tradingPair}" +
                                 $"&rate={rate.ToString(CultureInfo.InvariantCulture)}" +
                                 $"&amount={amount.ToString(CultureInfo.InvariantCulture)}";

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("Key", _poloniexOptions.ApiKey);
                client.DefaultRequestHeaders.Add("Sign", SignPostBody(formParameters));

                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("command", tradeType.ToString().ToLower()),
                    new KeyValuePair<string, string>("nonce", nonce.ToString()),
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

                    var responseDefinition = new { OrderNumber = long.MaxValue, ResultingTrades = new List<PoloniexTrade>() };
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent.ToLower().Contains("error"))
                    {
                        Console.WriteLine($"Create order error: Error: {responseContent}");
                        return -1;
                    }
                    var deserializedResponse = JsonConvert.DeserializeAnonymousType(responseContent, responseDefinition);
                    return deserializedResponse.OrderNumber;
                }
            }
        }

        private string SignPostBody(string formParameters)
        {
            var hmacHash = new HMACSHA512(Encoding.UTF8.GetBytes(_poloniexOptions.ApiSecret));
            var signedParametersByteArray = hmacHash.ComputeHash(Encoding.UTF8.GetBytes(formParameters));
            return BitConverter.ToString(signedParametersByteArray).Replace("-", "");
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
