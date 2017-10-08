using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using TradingTester.Logic.Models;
using TradingTester.Logic.Repositories.Interfaces;
using TradingTester.Logic.Services.Interfaces;

namespace TradingTester.Logic.Services
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

    public class CandlePeriodModel
    {
        public DateTime PeriodStart { get; set; }

        public DateTime PeriodEnd { get; set; }

        public long ScanId { get; set; }

        public IEnumerable<CandleModel> Candles { get; set; }
    }
}