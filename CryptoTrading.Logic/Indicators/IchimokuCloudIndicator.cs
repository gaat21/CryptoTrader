using System;
using System.Linq;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Utils;

namespace CryptoTrading.Logic.Indicators
{
    public class IchimokuCloudIndicator : IIndicator
    {
        private readonly FixedSizedQueue<CandleModel> _shortPeriodQueue;
        private readonly FixedSizedQueue<CandleModel> _middlePeriodQueue;
        private readonly FixedSizedQueue<CandleModel> _longPeriodQueue;
        private readonly FixedSizedQueue<decimal> _tenkenSenQueue;
        private readonly FixedSizedQueue<decimal> _kijunSenQueue;
        private readonly FixedSizedQueue<decimal> _longPeriodHighestHighQueue;
        private readonly FixedSizedQueue<decimal> _longPeriodLowestLowQueue;

        private const int Short = 20;
        private const int Middle = 60;
        private const int Long = 120;

        public IchimokuCloudIndicator()
        {
            _shortPeriodQueue = new FixedSizedQueue<CandleModel>(Short);
            _middlePeriodQueue = new FixedSizedQueue<CandleModel>(Middle);
            _longPeriodQueue = new FixedSizedQueue<CandleModel>(Long);
            _tenkenSenQueue = new FixedSizedQueue<decimal>(Middle);
            _kijunSenQueue = new FixedSizedQueue<decimal>(Middle);
            _longPeriodHighestHighQueue = new FixedSizedQueue<decimal>(Middle);
            _longPeriodLowestLowQueue = new FixedSizedQueue<decimal>(Middle);
        }

        public IndicatorModel GetIndicatorValue(CandleModel currentCandle)
        {
            _shortPeriodQueue.Enqueue(currentCandle);
            _middlePeriodQueue.Enqueue(currentCandle);
            _longPeriodQueue.Enqueue(currentCandle);

            if (_middlePeriodQueue.QueueSize == Middle)
            {
                var tenkanSenValue = Math.Round((_shortPeriodQueue.GetItems().Max(m => m.HighPrice) + _shortPeriodQueue.GetItems().Min(m => m.LowPrice)) / 2, 4);
                var kijunSenValue = Math.Round((_middlePeriodQueue.GetItems().Max(m => m.HighPrice) + _middlePeriodQueue.GetItems().Min(m => m.LowPrice)) / 2, 4);
                _tenkenSenQueue.Enqueue(tenkanSenValue);
                _kijunSenQueue.Enqueue(kijunSenValue);
            }

            if (_longPeriodQueue.QueueSize == Long)
            {
                _longPeriodHighestHighQueue.Enqueue(_longPeriodQueue.GetItems().Max(h => h.HighPrice));
                _longPeriodLowestLowQueue.Enqueue(_longPeriodQueue.GetItems().Min(h => h.LowPrice));
            }

            if (_longPeriodQueue.QueueSize + _longPeriodHighestHighQueue.QueueSize < Middle + Long)
            {
                return new IndicatorModel
                {
                    IndicatorValue = 0
                };
            }

            var ssaFutureMiddleQueue = new FixedSizedQueue<decimal>(Middle);
            var ssbFutureMiddleQueue = new FixedSizedQueue<decimal>(Middle);
            var crossOver = false;
            for (int i = 0; i < Middle; i++)
            {
                var ssaFutureMiddleValue = Math.Round((_tenkenSenQueue[i] + _kijunSenQueue[i]) / 2, 4);
                var ssbFutureMiddleValue = Math.Round((_longPeriodHighestHighQueue[i] + _longPeriodLowestLowQueue[i]) / 2, 4);

                if (ssaFutureMiddleValue > ssbFutureMiddleValue)
                {
                    crossOver = true;
                }

                ssaFutureMiddleQueue.Enqueue(ssaFutureMiddleValue);
                ssbFutureMiddleQueue.Enqueue(ssbFutureMiddleValue);
            }

            return new IndicatorModel
            {
                IchimokuCloud = new IchimokuCloudModel
                {
                    KijunSenValue = _kijunSenQueue.GetItems().Last(),
                    TenkanSenValue = _tenkenSenQueue.GetItems().Last(),
                    SenkouSpanAValue = ssaFutureMiddleQueue.GetItems().First(),
                    SenkouSpanBValue = ssbFutureMiddleQueue.GetItems().First(),
                    SsaFuture = ssaFutureMiddleQueue.GetItems(),
                    SsbFuture = ssbFutureMiddleQueue.GetItems(),
                    SsaCrossoverSsb = crossOver
                }
            };
        }

        public IndicatorModel GetIndicatorValue(decimal value)
        {
            throw new NotSupportedException();
        }
    }
}
