using TradingTester.Logic.Indicators.Interfaces;

namespace TradingTester.Logic.Indicators
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
        public decimal GetIndicatorValue(decimal price)
        {
            // weight factor
            decimal k = 2 / (_weight + 1);

            // yesterday
            decimal y = _previousIndicatorValue ?? price;

            // calculation
            _previousIndicatorValue = price * k + y * (1 - k);

            return decimal.Round(_previousIndicatorValue.Value, 8);
        }
    }
}
