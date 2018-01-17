using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Strategies.Interfaces;
using CryptoTrading.Logic.Utils;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Strategies
{
    public class EthMacdStrategy : IStrategy
    {
        private readonly IIndicator _shortEmaIndicator;
        private readonly IIndicator _longEmaIndicator;
        private readonly IIndicator _signalEmaIndicator;
        //private readonly IIndicator _tdiIndicator;

        private TrendDirection _lastTrend = TrendDirection.Short;
        private decimal _lastBuyPrice;
        private decimal _maxOrMinMacd;
        private decimal _minMacd;
        private decimal _minWarmupMacd;
        private decimal _maxMacd;
        private decimal _maxMacdClosePrice;
        private decimal _minMacdClosePrice;
        private decimal? _lastMacd;
        private readonly MacdStrategyOptions _options;
        private bool _stopTrading;
        private decimal _macdRate;
        private bool _macdSwitch;
        private int _candleCount = 1;
        private decimal _lastClosePrice;
        private readonly FixedSizedQueue<MacdStatistic> _macdStatistcsQueue;
        private readonly FixedSizedQueue<MacdStatistic> _macdTempStatisticsQueue;
        private MacdDirection _macdDirection = MacdDirection.GreaterThanZero;
        private readonly FixedSizedQueue<decimal> _volumenQueue;

        public EthMacdStrategy(IOptions<MacdStrategyOptions> options, IIndicatorFactory indicatorFactory)
        {
            _options = options.Value;
            _shortEmaIndicator = indicatorFactory.GetEmaIndicator(_options.ShortWeight);
            _longEmaIndicator = indicatorFactory.GetEmaIndicator(_options.LongWeight);
            _signalEmaIndicator = indicatorFactory.GetEmaIndicator(_options.Signal);

            //_tdiIndicator = indicatorFactory.GetTdiIndicator(_options.TdiPeriod);
            _macdStatistcsQueue = new FixedSizedQueue<MacdStatistic>(6);
            _macdTempStatisticsQueue = new FixedSizedQueue<MacdStatistic>(200);
            _volumenQueue = new FixedSizedQueue<decimal>(100);
        }

        public int CandleSize => 1;

        public async Task<TrendDirection> CheckTrendAsync(List<CandleModel> previousCandles, CandleModel currentCandle)
        {
            var shortEmaValue = _shortEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            var longEmaValue = _longEmaIndicator.GetIndicatorValue(currentCandle).IndicatorValue;
            //var tdiValue = _tdiIndicator.GetIndicatorValue(currentCandle.ClosePrice).IndicatorValue;
            var emaDiffValue = shortEmaValue - longEmaValue;
            var signalEmaValue = Math.Round(_signalEmaIndicator.GetIndicatorValue(emaDiffValue).IndicatorValue, 4);
            var macdValue = Math.Round(emaDiffValue - signalEmaValue, 4);

            var avgVolumen = Math.Round(_volumenQueue.GetItems().Average(), 8);

            Console.WriteLine($"DateTs: {currentCandle.StartDateTime:s}; " +
                              $"MACD: {macdValue};\t" +
                              $"Volumen avg: {avgVolumen}; Current volumen: {currentCandle.Volume};\t" +
                              $"Close price: {currentCandle.ClosePrice};");

            _volumenQueue.Enqueue(currentCandle.Volume);

            if (!_lastMacd.HasValue || _lastMacd == 0)
            {
                _lastMacd = macdValue;
                _lastClosePrice = currentCandle.ClosePrice;
                _macdDirection = macdValue < 0 ? MacdDirection.LessThanZero : MacdDirection.GreaterThanZero;
                return await Task.FromResult(TrendDirection.None);
            }

            CreateMacdStatistics(macdValue, currentCandle);

            if (_lastMacd < 0 && macdValue >= 0)
            {
                if (_macdSwitch)
                {
                    _maxMacd = 0;
                }
                else
                {
                    _macdSwitch = true;
                }
            }

            if (_lastMacd > 0 && macdValue <= 0)
            {
                if (_macdSwitch)
                {
                    _minMacd = 0;
                }
                else
                {
                    _macdSwitch = true;
                }
            }

            if (macdValue < 0 && macdValue < _minMacd)
            {
                _minMacd = macdValue;
                _minMacdClosePrice = currentCandle.ClosePrice;
            }

            if (macdValue > 0 && macdValue > _maxMacd)
            {
                _maxMacd = macdValue;
                _maxMacdClosePrice = currentCandle.ClosePrice;
            }

            // wait 1 hour
            if (_candleCount <= 60)
            {
                _candleCount++;
                if (macdValue < 0 && macdValue < _minWarmupMacd)
                {
                    _minWarmupMacd = macdValue;
                }
                Console.WriteLine($"Min warmup Macd: {_minWarmupMacd}");
                return await Task.FromResult(TrendDirection.None);
            }

            if (_lastTrend == TrendDirection.Short)
            {
                if (macdValue > 0 && _stopTrading)
                {
                    _stopTrading = false;
                }

                if (macdValue < 0 && macdValue < _lastMacd)
                {
                    _maxOrMinMacd = macdValue;
                }

                //if (macdValue < 0)
                //{
                //    _macdRate = (1 - Math.Round(_minMacdClosePrice / _maxMacdClosePrice, 4)) * 100;
                //    Console.WriteLine($"MaxMacd: {_maxMacdClosePrice}; MinMacd: {_minMacdClosePrice}; Rate: {_macdRate}%");
                //}

                var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_stopTrading == false
                    && macdValue < _options.BuyThreshold
                    //&& macdValue < _minWarmupMacd * (decimal)2.0
                    && diffPreviousMacd < -(decimal)0.1
                    && currentCandle.Volume >= avgVolumen * (decimal)1.5
                    && macdValue > _lastMacd
                    && currentCandle.ClosePrice > _lastClosePrice)
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;

                    _macdRate = (1 - Math.Round(_minMacdClosePrice / _maxMacdClosePrice, 4)) * 100;
                    Console.WriteLine($"MaxMacd: {_maxMacdClosePrice}; MinMacd: {_minMacdClosePrice}; Rate: {_macdRate}%");
                    if (_macdRate < (decimal)2.0 /*|| _macdRate > (decimal)6.0*/)
                    {
                        _stopTrading = true;
                        return await Task.FromResult(TrendDirection.None);
                    }

                    //var allStats = _macdStatistcsQueue.GetItems();
                    //if (allStats.Count == 6)
                    //{
                    //    Console.WriteLine($"Stat-1: {allStats[5]} - Stat-2: {allStats[4]}");
                    //    Console.WriteLine($"Stat-3: {allStats[3]} - Stat-4: {allStats[2]}");
                    //    Console.WriteLine($"Stat-5: {allStats[1]} - Stat-6: {allStats[0]}");
                    //    Console.WriteLine();
                    //}

                    _lastTrend = TrendDirection.Long;
                    _maxOrMinMacd = 0;
                    _lastBuyPrice = currentCandle.ClosePrice;
                }
                else
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(TrendDirection.None);
                }
            }
            else if (_lastTrend == TrendDirection.Long)
            {
                if (macdValue > 0 && macdValue > _lastMacd)
                {
                    _maxOrMinMacd = macdValue;
                }

                if (macdValue < 0)
                {
                    _maxOrMinMacd = 0;
                }

                var stopPercentage = 1 - (_macdRate - (decimal)0.4) / 100; //(decimal) 0.97;
                var profitPercentage = 1 + (_macdRate + (decimal)0.4) / 100; //(decimal) 1.038;
                var diffPreviousMacd = _maxOrMinMacd - macdValue;
                if (_lastMacd > macdValue
                    && diffPreviousMacd > (decimal)1.0
                    && (currentCandle.ClosePrice > _lastBuyPrice * profitPercentage
                        || currentCandle.ClosePrice < _lastBuyPrice * stopPercentage))
                {
                    Console.WriteLine($"Stop percentage: {stopPercentage}; Profit percentage: {profitPercentage}");
                    _lastTrend = TrendDirection.Short;
                    _maxOrMinMacd = 0;
                    _stopTrading = true;
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                }
                else
                {
                    _lastMacd = macdValue;
                    _lastClosePrice = currentCandle.ClosePrice;
                    return await Task.FromResult(TrendDirection.None);
                }
            }

            return await Task.FromResult(_lastTrend);
        }

        private void CreateMacdStatistics(decimal macdValue, CandleModel currentCandle)
        {
            var macdStatistic = new MacdStatistic
            {
                Macd = macdValue,
                Candle = currentCandle
            };

            if (macdValue > 0)
            {
                if (_macdDirection == MacdDirection.LessThanZero)
                {
                    _macdDirection = MacdDirection.GreaterThanZero;

                    var minMacd = _macdTempStatisticsQueue.GetItems().Min(s => s.Macd);
                    var stat = _macdTempStatisticsQueue.GetItems().First(f => f.Macd == minMacd);

                    stat.TrendCount = _macdTempStatisticsQueue.GetItems().Count;
                    _macdStatistcsQueue.Enqueue(stat);
                    _macdTempStatisticsQueue.Clear();
                }

                macdStatistic.Direction = _macdDirection;
                _macdTempStatisticsQueue.Enqueue(macdStatistic);
            }
            else
            {
                if (_macdDirection == MacdDirection.GreaterThanZero)
                {
                    _macdDirection = MacdDirection.LessThanZero;

                    var maxMacd = _macdTempStatisticsQueue.GetItems().Max(s => s.Macd);
                    var stat = _macdTempStatisticsQueue.GetItems().First(f => f.Macd == maxMacd);

                    stat.TrendCount = _macdTempStatisticsQueue.GetItems().Count;
                    _macdStatistcsQueue.Enqueue(stat);
                    _macdTempStatisticsQueue.Clear();
                }

                macdStatistic.Direction = _macdDirection;
                _macdTempStatisticsQueue.Enqueue(macdStatistic);
            }
        }
    }

    public class MacdStatistic
    {
        public CandleModel Candle { get; set; }

        public decimal Macd { get; set; }

        public int TrendCount { get; set; }

        public MacdDirection Direction { get; set; }

        public override string ToString()
        {
            return $"Macd: {Macd};\tTrendCount: {TrendCount};\tClosePrice: {Candle.ClosePrice};";
        }
    }

    public enum MacdDirection
    {
        LessThanZero,
        GreaterThanZero
    }
}
