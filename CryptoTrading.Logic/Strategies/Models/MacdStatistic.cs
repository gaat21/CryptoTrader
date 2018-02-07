using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Strategies.Models
{
    public class MacdStatistic
    {
        public CandleModel Candle { get; set; }

        public decimal Macd { get; set; }

        public int TrendCount { get; set; }

        public MacdDirection Direction { get; set; }

        public override string ToString()
        {
            return $"Macd: {Macd};\tTrendCount: {TrendCount};\tClosePrice: {Candle.ClosePrice};";
        }
    }
}