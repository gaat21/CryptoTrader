using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CryptoTrading.DAL.Models;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Repositories.Interfaces;

namespace CryptoTrading.Logic.Repositories
{
    public class ImportRepository : IImportRepository
    {
        private readonly IExchangeProvider _exchangeProvider;
        private readonly ICandleRepository _candleDbRepository;
        
        public ImportRepository(IExchangeProvider exchangeProvider, ICandleRepository candleDbRepository)
        {
            _exchangeProvider = exchangeProvider;
            _candleDbRepository = candleDbRepository;
        }

        public async Task<IEnumerable<CandleModel>> ImportCandlesAsync(string tradingPair, int intervalInHour, CandlePeriod candlePeriod)
        {
            var candles = new List<CandleModel>();
            var remainedHours = intervalInHour % 24;
            var initDateTime = DateTimeOffset.UtcNow.AddHours(-1 * intervalInHour);
            for (int i = 0; i < intervalInHour / 24; i++)
            {
                var startDateTime = initDateTime.AddDays(i);
                var endDateTime = initDateTime.AddDays(i + 1);
                candles.AddRange(await _exchangeProvider.GetCandlesAsync(tradingPair, candlePeriod, startDateTime.ToUnixTimeSeconds(), endDateTime.ToUnixTimeSeconds()));
                Console.WriteLine($"Iteration: {i}; StartDt: {startDateTime}; EndDt: {endDateTime};  Total count: {candles.Count}");
            }

            if (remainedHours > 0)
            {
                candles.AddRange(await _exchangeProvider.GetCandlesAsync(tradingPair, candlePeriod, DateTimeOffset.UtcNow.AddHours(-1 * remainedHours).ToUnixTimeSeconds(), null));
            }

            await _candleDbRepository.SaveCandleAsync(tradingPair, Mapper.Map<List<CandleDto>>(candles));
            
            return candles;
        }
    }
}
