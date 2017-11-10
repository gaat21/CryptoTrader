using System.Collections.Generic;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Indicators
{
    public class CandleSticksIndicator : IIndicator
    {
        public IndicatorModel GetIndicatorValue(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var result = new IndicatorModel();
            result.CandleFormat = CheckHammerCandleSticks(currentCandle);
            if (result.CandleFormat != CandleFormat.None)
            {
                return result;
            }
            result.CandleFormat = CheckMarubozuCandleSticks(currentCandle);
            if (result.CandleFormat != CandleFormat.None)
            {
                return result;
            }

            return result;
        }

        private CandleFormat CheckHammerCandleSticks(CandleModel candle)
        {
            return CandleFormat.None;
        }

        private CandleFormat CheckMarubozuCandleSticks(CandleModel candle)
        {
            if (candle.OpenPrice == candle.LowPrice 
                && candle.ClosePrice == candle.HighPrice)
            {
                return CandleFormat.BullishMarubozu;
            }

            if (candle.OpenPrice == candle.HighPrice 
                && candle.ClosePrice == candle.LowPrice)
            {
                return CandleFormat.BearishMarubozu;
            }

            return CandleFormat.None;
        }
    }
}
