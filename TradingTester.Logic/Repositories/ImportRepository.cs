using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using TradingTester.DAL.Models;
using TradingTester.Logic.Models;
using TradingTester.Logic.Providers.Interfaces;
using TradingTester.Logic.Repositories.Interfaces;

namespace TradingTester.Logic.Repositories
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

        public async Task<IEnumerable<CandleModel>> ImportCandlesAsync(string tradingPair, int intervalInHour, int candlePeriod)
        {
            var candles = await _exchangeProvider.GetCandlesAsync(tradingPair, DateTimeOffset.UtcNow.AddHours(-1 * intervalInHour).ToUnixTimeSeconds(), candlePeriod);

            await _candleDbRepository.SaveCandleAsync(tradingPair, Mapper.Map<List<CandleDto>>(candles));
            
            return candles;
        }
    }
}
