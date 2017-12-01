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

        public IndicatorModel GetIndicatorValue(CandleModel currentCandle)
        {
            return GetIndicatorValue(currentCandle.ClosePrice);
        }

        public IndicatorModel GetIndicatorValue(decimal value)
        {
            var momentum = value - _prevClosePrice;

            var innerEmaValue = _innerEmaIndicator.GetIndicatorValue(momentum);
            var outerEmaValue = _outerEmaIndicator.GetIndicatorValue(innerEmaValue.IndicatorValue);

            var absoluteInnerEmaValue = _absoluteInnerEmaIndicator.GetIndicatorValue(Math.Abs(momentum));
            var absoluteOuterEmaValue = _absoluteOuterEmaIndicator.GetIndicatorValue(absoluteInnerEmaValue.IndicatorValue);

            _prevClosePrice = value;

            return new IndicatorModel
            {
                IndicatorValue = 100 * outerEmaValue.IndicatorValue / absoluteOuterEmaValue.IndicatorValue
            };
        }
    }
}
