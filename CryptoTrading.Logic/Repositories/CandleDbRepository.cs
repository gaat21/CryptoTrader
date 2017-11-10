using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.DAL;
using CryptoTrading.DAL.Models;
using CryptoTrading.Logic.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CryptoTrading.Logic.Repositories
{
    public class CandleDbRepository : ICandleRepository
    {
        private readonly ITradingDbContext _tradonDbContext;

        public CandleDbRepository(ITradingDbContext tradonDbContext)
        {
            _tradonDbContext = tradonDbContext;
        }

        public async Task SaveCandleAsync(string tradingPair, List<CandleDto> candlesDto)
        {
            var lastScanId = GetLatestScanId();
            
            foreach (var candleDto in candlesDto)
            {
                candleDto.ScanId = lastScanId + 1;
                candleDto.TradingPair = tradingPair;
            }

            _tradonDbContext.Candles.AddRange(candlesDto);
            await _tradonDbContext.SaveChangesAsync();
        }

        public async Task<Dictionary<long, List<CandleDto>>> GetCandlesAsync(string tradingPair)
        {
           return await _tradonDbContext.Candles.Where(w => w.TradingPair == tradingPair).GroupBy(g => g.ScanId).ToDictionaryAsync(d => d.Key, d => d.ToList().OrderBy(o => o.StartDateTime).ToList());
        }

        private long GetLatestScanId()
        {
            return _tradonDbContext.Candles.Any() ? _tradonDbContext.Candles.GroupBy(g => g.ScanId).OrderBy(o => o.Key).Last().Key : 0;
        }
    }
}
