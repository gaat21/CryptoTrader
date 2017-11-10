using System.Collections.Generic;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Strategies.Interfaces;

namespace CryptoTrading.Logic.Strategies
{
    public class HammerStrategy : IStrategy
    {
        public int CandleSize => 1;

        public async Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            //var prevCandle = previousCandles.Last();

            //if (prevCandle.HighPrice == prevCandle.ClosePrice)
            //{
                
            //}
            return await Task.FromResult(TrendDirection.None);
        }
    }
}
