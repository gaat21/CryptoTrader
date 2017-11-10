namespace CryptoTrading.Logic.Models
{
    public class IndicatorModel
    {
        public decimal IndicatorValue { get; set; }

        public CandleFormat CandleFormat { get; set; } = CandleFormat.None;
    }
}