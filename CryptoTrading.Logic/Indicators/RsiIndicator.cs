using System;
using System.Collections.Generic;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Indicators
{
    public class RsiIndicator : IIndicator
    {
        private static Queue<decimal> _prevClosePrices;
        private readonly int _weight;
        private decimal _upTrendAvg;
        private decimal _downTrendAvg;

        public RsiIndicator(int weight)
        {
            _weight = weight;
            _prevClosePrices = new Queue<decimal>(weight);
        }

        public IndicatorModel GetIndicatorValue(CandleModel currentCandle)
        {
            return GetIndicatorValue(currentCandle.ClosePrice);
        }

        public IndicatorModel GetIndicatorValue(decimal value)
        {
            if (_prevClosePrices.Count < _weight)
            {
                _prevClosePrices.Enqueue(value);
            }

            if (_prevClosePrices.Count == _weight)
            {
                _prevClosePrices.Dequeue();
                _prevClosePrices.Enqueue(value);

                GetAverageValue();
                var rsiValue = 100 - 100 / (1 + _upTrendAvg / _downTrendAvg);
                return new IndicatorModel
                {
                    IndicatorValue = Math.Round(rsiValue)
                };
            }

            return new IndicatorModel
            {
                IndicatorValue = 0
            };
        }

        public void GetAverageValue()
        {
            decimal upTrendValue = 0;
            var upTrendCount = 0;
            decimal downTrendValue = 0;
            var downTrendCount = 0;
            var prevClosePriceArray = _prevClosePrices.ToArray();
            for (int i = 1; i < prevClosePriceArray.Length; i++)
            {
                var diff = prevClosePriceArray[i] - prevClosePriceArray[i - 1];
                if (diff < 0)
                {
                    downTrendValue += diff * -1;
                    downTrendCount += 1;
                }
                else
                {
                    upTrendValue += diff;
                    upTrendCount += 1;
                }
            }

            _upTrendAvg = upTrendCount > 0 ? upTrendValue / upTrendCount : 1;
            _downTrendAvg = downTrendCount > 0 ? downTrendValue / downTrendCount : 1;
        }
    }
}
