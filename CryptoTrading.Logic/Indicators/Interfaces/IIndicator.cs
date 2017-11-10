using System.Collections.Generic;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Indicators.Interfaces
{
    public interface IIndicator
    {
        IndicatorModel GetIndicatorValue(List<CandleModel> previousCandles, CandleModel currentCandle);
    }
}
