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
            var candles = await _exchangeProvider.GetCandlesAsync(tradingPair, DateTimeOffset.UtcNow.AddHours(-1 * intervalInHour).ToUnixTimeSeconds(), candlePeriod);

            await _candleDbRepository.SaveCandleAsync(tradingPair, Mapper.Map<List<CandleDto>>(candles));
            
            return candles;
        }
    }
}
