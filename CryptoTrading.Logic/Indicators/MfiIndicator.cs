using System;
using System.Collections.Generic;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;

namespace CryptoTrading.Logic.Indicators
{
    public class MfiIndicator : IIndicator
    {
        private readonly int _weight;
        private readonly Queue<decimal> _moneyFlowQueue;
        private decimal _lastTypicalPrice;

        public MfiIndicator(int weight)
        {
            _weight = weight;
            _moneyFlowQueue = new Queue<decimal>(weight);
        }

        public IndicatorModel GetIndicatorValue(CandleModel currentCandle)
        {
            var typicalPrice = (currentCandle.HighPrice + currentCandle.LowPrice + currentCandle.ClosePrice) / 3;
            var rowMoneyFlow = typicalPrice * currentCandle.Volume;

            decimal moneyFlowValue;
            if (_lastTypicalPrice == typicalPrice)
            {
                moneyFlowValue = 0;
            }
            else
            {
                moneyFlowValue = _lastTypicalPrice < typicalPrice ? rowMoneyFlow : -1 * rowMoneyFlow;
            }

            if (_moneyFlowQueue.Count < _weight)
            {
                _moneyFlowQueue.Enqueue(moneyFlowValue);
            }
            if (_moneyFlowQueue.Count == _weight)
            {
                _moneyFlowQueue.Dequeue();
                _moneyFlowQueue.Enqueue(moneyFlowValue);

                var moneyRatio = CalculateMoneyRation();

                var mfi = 100 - 100 / (1 + moneyRatio);

                return new IndicatorModel
                {
                    IndicatorValue = Math.Round(mfi, 2)
                };
            }

            _lastTypicalPrice = typicalPrice;

            return new IndicatorModel
            {
                IndicatorValue = -1
            };
        }

        public IndicatorModel GetIndicatorValue(decimal value)
        {
            throw new NotImplementedException();
        }

        private decimal CalculateMoneyRation()
        {
            decimal positiveMoneyFlow = 1;
            decimal negativeMoneyFlow = 1;

            foreach (var moneyFlowValue in _moneyFlowQueue.ToArray())
            {
                if (moneyFlowValue < 0)
                {
                    negativeMoneyFlow += -1 * moneyFlowValue;
                }
                else
                {
                    positiveMoneyFlow += moneyFlowValue;
                }
            }

            return positiveMoneyFlow / negativeMoneyFlow;
        }
    }
}
