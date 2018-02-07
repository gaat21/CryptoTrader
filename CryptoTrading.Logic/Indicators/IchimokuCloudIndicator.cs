using System;
using System.Linq;
using System.Reflection.Metadata;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Utils;

namespace CryptoTrading.Logic.Indicators
{
    public class IchimokuCloudIndicator : IIndicator
    {
        private readonly FixedSizedQueue<decimal> _period9Queue;
        private readonly FixedSizedQueue<decimal> _period26Queue;
        private readonly FixedSizedQueue<decimal> _period52Queue;

        public IchimokuCloudIndicator()
        {
            _period9Queue = new FixedSizedQueue<decimal>(9);
            _period26Queue = new FixedSizedQueue<decimal>(26);
            _period52Queue = new FixedSizedQueue<decimal>(52);
        }

        public IndicatorModel GetIndicatorValue(CandleModel currentCandle)
        {
            return GetIndicatorValue(currentCandle.ClosePrice);
        }

        public IndicatorModel GetIndicatorValue(decimal value)
        {
            _period9Queue.Enqueue(value);
            _period26Queue.Enqueue(value);
            _period52Queue.Enqueue(value);

            if (_period52Queue.QueueSize < 52)
            {
                return new IndicatorModel
                {
                    IndicatorValue = 0
                };
            }

            var tenkanSenValue = Math.Round((_period9Queue.GetItems().Max() + _period9Queue.GetItems().Min()) / 2, 4);
            var kijunSenValue = Math.Round((_period26Queue.GetItems().Max() + _period26Queue.GetItems().Min()) / 2, 4);
            return new IndicatorModel
            {
                IchimokuCloud = new IchimokuCloudModel
                {
                    KijunSenValue = kijunSenValue,
                    TenkanSenValue = tenkanSenValue,
                    SenkouSpanAValue = Math.Round((kijunSenValue + tenkanSenValue) / 2, 4),
                    SenkouSpanBValue = Math.Round((_period52Queue.GetItems().Max() + _period52Queue.GetItems().Min()) / 2, 4)
        }
            };
        }
    }
}
