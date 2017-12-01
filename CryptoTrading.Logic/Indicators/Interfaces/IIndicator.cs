using System.Collections.Generic;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Indicators.Interfaces
{
    public interface IIndicator
    {
        IndicatorModel GetIndicatorValue(CandleModel currentCandle);

        IndicatorModel GetIndicatorValue(decimal value);
    }
}
