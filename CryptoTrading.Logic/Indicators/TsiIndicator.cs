using System;
using System.Collections.Generic;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Indicators
{
    public class TsiIndicator : IIndicator
    {
        private readonly EmaIndicator _innerEmaIndicator;
        private readonly EmaIndicator _outerEmaIndicator;
        private readonly EmaIndicator _absoluteInnerEmaIndicator;
        private readonly EmaIndicator _absoluteOuterEmaIndicator;

        private static decimal _prevClosePrice;

        public TsiIndicator(IOptions<EmaStrategyOptions> emaOptions)
        {
            _innerEmaIndicator = new EmaIndicator(emaOptions.Value.LongWeight);
            _outerEmaIndicator = new EmaIndicator(emaOptions.Value.ShortWeight);
            _absoluteInnerEmaIndicator = new EmaIndicator(emaOptions.Value.LongWeight);
            _absoluteOuterEmaIndicator = new EmaIndicator(emaOptions.Value.ShortWeight);
        }

        public IndicatorModel GetIndicatorValue(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var momentum = currentCandle.ClosePrice - _prevClosePrice;

            var innerEmaValue = _innerEmaIndicator.GetIndicatorValue(null, new CandleModel{ ClosePrice = momentum });
            var outerEmaValue = _outerEmaIndicator.GetIndicatorValue(null, new CandleModel { ClosePrice = innerEmaValue.IndicatorValue });

            var absoluteInnerEmaValue = _absoluteInnerEmaIndicator.GetIndicatorValue(null, new CandleModel { ClosePrice = Math.Abs(momentum) });
            var absoluteOuterEmaValue = _absoluteOuterEmaIndicator.GetIndicatorValue(null, new CandleModel { ClosePrice = absoluteInnerEmaValue.IndicatorValue });

            _prevClosePrice = currentCandle.ClosePrice;

            return new IndicatorModel
            {
                IndicatorValue = 100 * outerEmaValue.IndicatorValue / absoluteOuterEmaValue.IndicatorValue
            };
        }
    }
}
