using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Repositories.Interfaces;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Services.Models;

namespace CryptoTrading.Logic.Services
{
    public class CandleDbService : ICandleService
    {
        private readonly ICandleRepository _candleDbRepository;

        public CandleDbService(ICandleRepository candleDbRepository)
        {
            _candleDbRepository = candleDbRepository;
        }

        public async Task<IEnumerable<CandlePeriodModel>> GetAvailableCandlePeriodsAsync(string tradingPair)
        {
            var availableCandlePeriods = await _candleDbRepository.GetCandlesAsync(tradingPair);

            var candlePeriods = new List<CandlePeriodModel>();
            foreach (var availableCandlePeriod in availableCandlePeriods)
            {
                var orderedCandles = availableCandlePeriod.Value.OrderBy(o => o.StartDateTime);
                candlePeriods.Add(new CandlePeriodModel
                {
                    ScanId = availableCandlePeriod.Key,
                    PeriodStart = orderedCandles.First().StartDateTime,
                    PeriodEnd = orderedCandles.Last().StartDateTime,
                    Candles = Mapper.Map<IEnumerable<CandleModel>>(orderedCandles)
                });
            }

            return candlePeriods;
        }
    }
}