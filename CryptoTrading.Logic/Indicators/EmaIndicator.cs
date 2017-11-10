using System.Collections.Generic;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Indicators
{
    public class EmaIndicator : IIndicator
    {
        private readonly decimal _weight;
        
        public EmaIndicator(int weight)
        {
            _weight = weight;
        }

        private decimal? _previousIndicatorValue;

        //  calculation (based on tick/day):
        //  EMA = Price(t) * k + EMA(y) * (1 – k)
        //  t = today, y = yesterday, N = number of days in EMA, k = 2 / (N+1)
        public IndicatorModel GetIndicatorValue(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            decimal price = currentCandle.ClosePrice;

            // weight factor
            decimal k = 2 / (_weight + 1);

            // yesterday
            decimal y = _previousIndicatorValue ?? price;

            // calculation
            _previousIndicatorValue = price * k + y * (1 - k);

            return new IndicatorModel {IndicatorValue = decimal.Round(_previousIndicatorValue.Value, 8)};
        }
    }
}
