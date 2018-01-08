using System;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Utils;

namespace CryptoTrading.Logic.Indicators
{
    public class TdiIndicator : IIndicator
    {
        private readonly int _period;
        private readonly FixedSizedQueue<decimal> _fixedSizedQueue;

        public TdiIndicator(int period)
        {
            _period = period;
            _fixedSizedQueue = new FixedSizedQueue<decimal>(2 * _period);
        }

        public IndicatorModel GetIndicatorValue(CandleModel currentCandle)
        {
            return GetIndicatorValue(currentCandle.ClosePrice);
        }

        public IndicatorModel GetIndicatorValue(decimal value)
        {
            //Mom = Price - Price[Period] 
            //MomAbs = Abs(Mom) 
            //MomSum = Sum(Mom, Period) 
            //MomSumAbs = Abs(MomSum) 
            //MomAbsSum = Sum(MomAbs, Period) 
            //MomAbsSum2 = Sum(MomAbs, Period * 2) 
            //TDI = MomSumAbs - (MomAbsSum2 - MomAbsSum)

            _fixedSizedQueue.Enqueue(value);
            if (_fixedSizedQueue.QueueSize <= _period)
            {
                return new IndicatorModel
                {
                    IndicatorValue = 0
                };
            }

            var mom = value - _fixedSizedQueue[_period];
            var momAbs = Math.Abs(mom);
            var momSum = mom + _period;
            var momSumAbs = Math.Abs(momSum);
            var momAbsSum = momAbs + _period;
            var momAbsSum2 = momAbs + _period * 2;

            return new IndicatorModel
            {
                IndicatorValue = momSumAbs - (momAbsSum2 - momAbsSum)
            };
        }
    }
}
