using System;
using System.Collections.Generic;
using System.Linq;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;

namespace CryptoTrading.Logic.Utils
{
    public static class CandleBatcher
    {
        public static IEnumerable<CandleModel> MergeCandleDtos(List<PoloniexTrade> trades, long start, long end)
        {
            var orderedTrades = trades.OrderBy(o => o.Date);

            var candles = new List<CandleModel>();
            var startPeriod = start;
            while (startPeriod < end)
            {
                var tradeList = orderedTrades.Where(w => w.DateTs >= startPeriod && w.DateTs < startPeriod + 60).ToList();
                if (tradeList.Count != 0)
                {
                    candles.Add(new CandleModel
                    {
                        StartDateTime = DateTimeOffset.FromUnixTimeSeconds(startPeriod).DateTime,
                        HighPrice = tradeList.Max(m => m.Rate),
                        LowPrice = tradeList.Min(m => m.Rate),
                        ClosePrice = tradeList.Last().Rate,
                        OpenPrice = tradeList.First().Rate,
                        TradingCount = tradeList.Count,
                        Volume = decimal.Round(tradeList.Sum(s => s.Amount), 8),
                        VolumeWeightedPrice = decimal.Round(tradeList.Sum(s => s.Amount) / tradeList.Count, 8)
                    });
                }

                startPeriod += 60;
            }

            return candles;
        }
    }
}
