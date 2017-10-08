using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TradingTester.Logic.Models;
using TradingTester.Logic.Options;
using TradingTester.Logic.Providers.Interfaces;
using TradingTester.Logic.Providers.Models;

namespace TradingTester.Logic.Providers
{
    public class PoloniexExchangeProvider : HttpBaseProvider, IExchangeProvider
    {
        public PoloniexExchangeProvider(IOptions<PoloniexOptions> poloniexOptions) : base(poloniexOptions.Value.ApiUrl)
        {
        }

        public async Task<IEnumerable<CandleModel>> GetCandlesAsync(string tradingPair, long since, int intervalInMin)
        {
            using (var client = GetClient())
            {
                var endPointUrl = $"/public?command=returnChartData&currencyPair={tradingPair}&start={since}&end=9999999999&period={intervalInMin * 60}";

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
    }
}
