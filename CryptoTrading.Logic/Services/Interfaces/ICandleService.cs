using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Services.Models;

namespace CryptoTrading.Logic.Services.Interfaces
{
    public interface ICandleService
    {
        Task<IEnumerable<CandlePeriodModel>> GetAvailableCandlePeriodsAsync(string tradingPair);
    }
}