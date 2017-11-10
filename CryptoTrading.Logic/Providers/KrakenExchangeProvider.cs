using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CryptoTrading.Logic.Providers
{
    public class KrakenExchangeProvider : HttpBaseProvider, IExchangeProvider
    {        
        public KrakenExchangeProvider(IOptions<KrakenOptions> krakenOptions) : base(krakenOptions.Value.ApiUrl)
        {
        }

        public async Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, long start, CandlePeriod candlePeriod)
        {
            using (var client = GetClient())
            {
                var endPointUrl = $"/0/public/OHLC?pair={tradingPair}&interval={candlePeriod}&since={start}";

                using (var response = await client.GetAsync(endPointUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Response code: {response.StatusCode}");
                    }

                    var resultResponseContent = await response.Content.ReadAsStringAsync();
                    var obj = (JObject)JsonConvert.DeserializeObject(resultResponseContent);
                    var err = (JArray)obj["error"];
                    if (err.Count != 0)
                        throw new Exception(err[0].ToString());

                    var result = obj["result"].Value<JObject>();

                    var krakenResult = new KrakenResult
                    {
                        Candles = new Dictionary<string, List<KrakenOhlc>>()
                    };

                    foreach (var o in result)
                    {
                        if (o.Key == "last")
                        {
                            krakenResult.Last = o.Value.Value<long>();
                        }
                        else
                        {
                            var ohlc = o.Value.ToObject<decimal[][]>()
                                .Select(v => new KrakenOhlc
                                {
                                    StartDateTime = DateTimeOffset.FromUnixTimeSeconds((long)v[0]).DateTime,
                                    OpenPrice = v[1],
                                    HighPrice = v[2],
                                    LowPrice = v[3],
                                    ClosePrice = v[4],
                                    VolumeWeightedPrice = v[5],
                                    Volume = v[6],
                                    TradingCount = (int) v[7]
                                })
                                .ToList();
                            krakenResult.Candles.Add(o.Key, ohlc);
                        }
                    }

                    return Mapper.Map<IEnumerable<CandleModel>>(krakenResult.Candles[tradingPair]);                    
                }
            }
        }
    }
}
