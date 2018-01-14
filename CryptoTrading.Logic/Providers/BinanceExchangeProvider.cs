using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
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
        private readonly BinanceOptions _binanceOptions;
        private const int RecvWindow = 10000; // 10 seconds to receive the reqest to exchange server

        public BinanceExchangeProvider(IOptions<BinanceOptions> binanceOptions) : base(binanceOptions.Value.ApiUrl)
        {
            _binanceOptions = binanceOptions.Value;
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

        public async Task<long> CreateOrderAsync(TradeType tradeType, string tradingPair, decimal rate, decimal amount)
        {
            var createdTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var formParameters = $"symbol={tradingPair}" +
                                 $"&side={tradeType.ToString().ToUpper()}" +
                                 "&type=limit&timeInForce=GTC" +
                                 $"&quantity={rate.ToString(CultureInfo.InvariantCulture)}" +
                                 $"&price={amount.ToString(CultureInfo.InvariantCulture)}" +
                                 $"&recvWindow={RecvWindow}" +
                                 $"&timestamp={createdTs}";

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _binanceOptions.ApiKey);
                var signature = SignPostBody(formParameters);

                var requestBody = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("symbol", tradingPair),
                    new KeyValuePair<string, string>("side", tradeType.ToString().ToLower()),
                    new KeyValuePair<string, string>("type", "limit"),
                    new KeyValuePair<string, string>("timeInForce", "GTC"),
                    new KeyValuePair<string, string>("quantity", rate.ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>("price", amount.ToString(CultureInfo.InvariantCulture)),
                    new KeyValuePair<string, string>("recvWindow", RecvWindow.ToString()),
                    new KeyValuePair<string, string>("timestamp", createdTs.ToString()),
                    new KeyValuePair<string, string>("signature", signature)
                });

                using (var response = await client.PostAsync("/api/v3/order/test", requestBody))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var responseDefinition = new { OrderId = long.MaxValue };
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent.ToLower().Contains("error"))
                    {
                        Console.WriteLine($"Create order error: Error: {responseContent}");
                        return -1;
                    }
                    var deserializedResponse = JsonConvert.DeserializeAnonymousType(responseContent, responseDefinition);
                    return deserializedResponse.OrderId;
                }
            }
        }

        public async Task<bool> CancelOrderAsync(string tradingPair, long orderNumber)
        {
            var createdTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var formParameters = $"symbol={tradingPair}" +
                                 $"&orderId={orderNumber}" +
                                 $"&recvWindow={RecvWindow}" +
                                 $"&timestamp={createdTs}";

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _binanceOptions.ApiKey);
                var signature = SignPostBody(formParameters);

                using (var response = await client.DeleteAsync($"/api/v3/order?{formParameters}&signature={signature}"))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent.ToLower().Contains("error"))
                    {
                        Console.WriteLine($"Create order error: Error: {responseContent}");
                        return false;
                    }
                    return true;
                }
            }
        }

        public async Task<IEnumerable<OrderDetail>> GetOrderAsync(string tradingPair, long orderNumber)
        {
            var createdTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var formParameters = $"symbol={tradingPair}" +
                                 $"&orderId={orderNumber}" +
                                 $"&recvWindow={RecvWindow}" +
                                 $"&timestamp={createdTs}";

            using (var client = GetClient())
            {
                client.DefaultRequestHeaders.Add("X-MBX-APIKEY", _binanceOptions.ApiKey);
                var signature = SignPostBody(formParameters);

                using (var response = await client.GetAsync($"/api/v3/order?{formParameters}&signature={signature}"))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var responseDefinition = new { OrderId = long.MaxValue, Status = BinanceStatus.New };
                    var responseContent = await response.Content.ReadAsStringAsync();
                    if (responseContent.ToLower().Contains("error"))
                    {
                        Console.WriteLine($"Create order error: Error: {responseContent}");
                        return null;
                    }
                    var deserializedResponse = JsonConvert.DeserializeAnonymousType(responseContent, responseDefinition);
                    return deserializedResponse.Status == BinanceStatus.Filled
                        ? new List<OrderDetail>
                        {
                            new OrderDetail
                            {
                                CurrencyPair = tradingPair
                            }
                        }
                        : null;
                }
            }
        }

        public async Task<Ticker> GetTicker(string tradingPair)
        {
            using (var client = GetClient())
            {
                using (var response = await client.GetAsync($"/api/v3/ticker/bookTicker?symbol={tradingPair}"))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var responseDefinition = new { Symbol = "", BidPrice = (decimal)0.0, AskPrice = (decimal)0.0 };
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var deserializedResponse = JsonConvert.DeserializeAnonymousType(responseContent, responseDefinition);
                    return new Ticker
                    {
                        HighestBid = deserializedResponse.BidPrice,
                        LowestAsk = deserializedResponse.AskPrice
                    };
                }
            }
        }

        private string SignPostBody(string formParameters)
        {
            var hmacHash = new HMACSHA512(Encoding.UTF8.GetBytes(_binanceOptions.ApiSecret));
            var signedParametersByteArray = hmacHash.ComputeHash(Encoding.UTF8.GetBytes(formParameters));
            return BitConverter.ToString(signedParametersByteArray).Replace("-", "");
        }
    }

    public enum BinanceStatus
    {
        New,
        PartiallyFilled,
        Filled,
        Canceled,
        PendingCancel,
        Rejected,
        Expired
    }
}
