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
        private int _tradingCount;
        private bool _hasOpenPosition;
        
        public BacktestTraderService(IStrategy strategy, IUserBalanceService userBalanceService)
        {
            _strategy = strategy;
            _userBalanceService = userBalanceService;
        }

        public int TradingCount => _tradingCount;

        public async Task CheckStrategyAsync(List<CandleModel> candles)
        {
            if (candles.Count == 0)
            {
                Console.WriteLine("No candles!!");
                return;
            }

            _userBalanceService.FirstPrice = candles.First().ClosePrice;
            for (int i = 0; i < candles.Count; i++)
            {
                var startIndex = i - _strategy.CandleSize;
                var prevCandles = candles.GetRange(startIndex < 0 ? 0 : startIndex, _strategy.CandleSize);
                var currentCandle = candles[i];
                var trendDirection = await _strategy.CheckTrendAsync(prevCandles, currentCandle);

                Console.WriteLine($"({i + 1}) - DateTs: {currentCandle.StartDateTime:s}; Trend: {trendDirection}; Open: {currentCandle.OpenPrice}; Close: {currentCandle.ClosePrice}; Low: {currentCandle.LowPrice}; High: {currentCandle.HighPrice}; Volumen: {currentCandle.Volume}");
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

            _userBalanceService.LastPrice = candles.Last().ClosePrice;
        }

        public Task StartTradingAsync(string tradingPair, CandlePeriod candlePeriod, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task SellAsync(CandleModel candle)
        {
            _hasOpenPosition = false;
            Console.WriteLine($"Sell crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");
            _tradingCount++;
            Console.WriteLine($"Profit: ${_userBalanceService.GetProfit(candle.ClosePrice)}");

            return Task.FromResult(0);
        }

        public Task BuyAsync(CandleModel candle)
        {
            _hasOpenPosition = true;
            _userBalanceService.SetBuyPrice(candle.ClosePrice);
            Console.WriteLine($"Buy crypto currency. Price: ${candle.ClosePrice}. Date: {candle.StartDateTime}");

            return Task.FromResult(0);
        }
    }
}
