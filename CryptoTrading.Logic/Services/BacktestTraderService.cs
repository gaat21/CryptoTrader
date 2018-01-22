using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Strategies.Interfaces;

namespace CryptoTrading.Logic.Services
{
    public class BacktestTraderService : ITraderService
    {
        private readonly IStrategy _strategy;
        private readonly IUserBalanceService _userBalanceService;
        private bool _hasOpenPosition;
        
        public BacktestTraderService(IStrategy strategy, IUserBalanceService userBalanceService)
        {
            _strategy = strategy;
            _userBalanceService = userBalanceService;
        }

        public async Task CheckStrategyAsync(string tradingPair, List<CandleModel> candles)
        {
            if (candles.Count == 0)
            {
                Console.WriteLine("No candles!!");
                return;
            }

            _userBalanceService.FirstPrice = candles.First();
            for (int i = 0; i < candles.Count; i++)
            {
                var startIndex = i - _strategy.CandleSize;
                var prevCandles = candles.GetRange(startIndex < 0 ? 0 : startIndex, _strategy.CandleSize);
                var currentCandle = candles[i];
                var trendDirection = await _strategy.CheckTrendAsync(tradingPair, prevCandles, currentCandle);

                //Console.WriteLine($"({i + 1}) - DateTs: {currentCandle.StartDateTime:s}; Trend: {trendDirection}; Open: {currentCandle.OpenPrice}; Close: {currentCandle.ClosePrice}; Low: {currentCandle.LowPrice}; High: {currentCandle.HighPrice}; Volumen: {currentCandle.Volume}");
                if (trendDirection == TrendDirection.None)
                {
                    if (i == candles.Count - 1 && _hasOpenPosition)
                    {
                        await SellAsync(currentCandle);
                    }
                    continue;
                }

                if (trendDirection == TrendDirection.Long)
                {
                    await BuyAsync(currentCandle);
                    continue;
                }
                await SellAsync(currentCandle);
            }

            _userBalanceService.LastPrice = candles.Last();
        }

        public Task StartTradingAsync(string tradingPair, CandlePeriod candlePeriod, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SellAsync(CandleModel candle)
        {
            _hasOpenPosition = false;
            _userBalanceService.TradingCount++;
            var profit = _userBalanceService.GetProfit(candle.ClosePrice, candle.StartDateTime);
            var msg = $"Sell crypto currency. Date: {candle.StartDateTime}; Price: ${candle.ClosePrice}; Rate: {_userBalanceService.Rate}\n" +
                      $"Profit: ${profit.Profit}\n" +
                      $"Trading time in hours: {Math.Round(profit.TradingTimeInMinutes / 60.0, 2)}\n";

            Console.WriteLine(msg);

            return Task.FromResult(0);
        }

        public Task BuyAsync(CandleModel candle)
        {
            _hasOpenPosition = true;
            _userBalanceService.SetBuyPrice(candle);
            Console.WriteLine($"Buy crypto currency. Date: {candle.StartDateTime}; Price: ${candle.ClosePrice}; Rate: {_userBalanceService.Rate}\n");

            return Task.FromResult(0);
        }
    }
}
