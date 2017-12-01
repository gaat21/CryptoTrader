using System.Collections.Generic;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Indicators
{
    public class CandleSticksIndicator : IIndicator
    {
        public IndicatorModel GetIndicatorValue(CandleModel currentCandle)
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
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

        public IndicatorModel GetIndicatorValue(decimal value)
        {
            throw new System.NotImplementedException();
        }

        private CandleFormat CheckHammerCandleSticks(CandleModel candle)
        {
            if (candle.ClosePrice > candle.OpenPrice)
            {
                // Bullish
                var bodySize = candle.ClosePrice - candle.OpenPrice;
                var lowerShadow = candle.OpenPrice - candle.LowPrice;
                var upperShadow = candle.HighPrice - candle.ClosePrice;


            }
            if (candle.OpenPrice > candle.ClosePrice)
            {
                // Bearish
                var bodySize = candle.OpenPrice - candle.ClosePrice;
                var lowerShadow = candle.ClosePrice - candle.LowPrice;
                var upperShadow = candle.HighPrice - candle.OpenPrice;
            }

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
