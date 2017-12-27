using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CryptoTrading.DAL.Models;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Repositories.Interfaces;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Strategies.Interfaces;
using Newtonsoft.Json;

namespace CryptoTrading.Logic.Services
{
    public class RealTimeTraderService : ITraderService
    {
        private readonly IStrategy _strategy;
        private readonly IUserBalanceService _userBalanceService;
        private readonly IExchangeProvider _exchangeProvider;
        private readonly ICandleRepository _candleRepository;
        private readonly IEmailService _emailService;
        private int _delayInMilliseconds = 60000;
        private string _tradingPair;

        private bool _isSetFirstPrice;

        private static TrendDirection _lastTrendDirection;
        private CancellationToken _cancellationToken;

        public RealTimeTraderService(IStrategy strategy,
                                     IUserBalanceService userBalanceService,
                                     IExchangeProvider exchangeProvider, 
                                     ICandleRepository candleRepository,
                                     IEmailService emailService)
        {
            _strategy = strategy;
            _userBalanceService = userBalanceService;
            _exchangeProvider = exchangeProvider;
            _candleRepository = candleRepository;
            _emailService = emailService;
        }

        public async Task CheckStrategyAsync(List<CandleModel> candles)
        {
            for (int i = 0; i < candles.Count; i++)
            {
                var startIndex = i - _strategy.CandleSize;
                var prevCandles = candles.GetRange(startIndex < 0 ? 0 : startIndex, _strategy.CandleSize);
                var currentCandle = candles[i];
                var trendDirection = await _strategy.CheckTrendAsync(prevCandles, currentCandle);

                if (trendDirection == TrendDirection.None)
                {
                    continue;
                }

                if (trendDirection == TrendDirection.Long)
                {
                    await BuyAsync(currentCandle);
                    continue;
                }
                await SellAsync(currentCandle);
            }
        }

        public async Task StartTradingAsync(string tradingPair, CandlePeriod candlePeriod, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
            _tradingPair = tradingPair;
            _delayInMilliseconds = (int) candlePeriod * _delayInMilliseconds;
            var lastSince = GetSinceUnixTime(candlePeriod);
            var lastScanId = _candleRepository.GetLatestScanId();
            CandleModel currentCandle = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                var startWatcher = new Stopwatch();
                startWatcher.Start();
                var candles = await _exchangeProvider.GetCandlesAsync(tradingPair, candlePeriod, lastSince, lastSince + (int)candlePeriod * 60);
                var candlesList = candles.ToList();
                if (candlesList.Count == _strategy.CandleSize)
                {
                    var prevCandles = candlesList.GetRange(0, candlesList.Count - 1);
                    currentCandle = candlesList.Last();
                    if (!_isSetFirstPrice)
                    {
                        _userBalanceService.FirstPrice = currentCandle;
                        _isSetFirstPrice = true;
                    }
                    var trendDirection = await _strategy.CheckTrendAsync(prevCandles, currentCandle);

                    await _candleRepository.SaveCandleAsync(tradingPair, Mapper.Map<List<CandleDto>>(new List<CandleModel> {currentCandle}), lastScanId);

                    Console.WriteLine($"DateTs: {DateTimeOffset.FromUnixTimeSeconds(lastSince):s}; Trend: {trendDirection}; Close price: {currentCandle.ClosePrice}; Volumen: {currentCandle.Volume}; Elapsed time: {startWatcher.ElapsedMilliseconds} ms");
                    _lastTrendDirection = trendDirection;
                    if (trendDirection == TrendDirection.Long)
                    {
                        await BuyAsync(currentCandle);
                    }
                    else if (trendDirection == TrendDirection.Short)
                    {
                        await SellAsync(currentCandle);
                    }
                }
                else
                {
                    Console.WriteLine($"DateTs: {DateTimeOffset.FromUnixTimeSeconds(lastSince):s}; Trend: [NO TRADES]; Close price: [NO TRADES]; Volumen: [NO TRADES]; Elapsed time: {startWatcher.ElapsedMilliseconds} ms");
                }

                // ReSharper disable once MethodSupportsCancellation
                await Task.Delay((int)(_delayInMilliseconds - startWatcher.ElapsedMilliseconds));
                lastSince += (int) candlePeriod * _strategy.CandleSize * 60;
                startWatcher.Stop();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                if (currentCandle != null)
                {
                    _userBalanceService.LastPrice = currentCandle;
                    if (_lastTrendDirection == TrendDirection.Long)
                    {
                        // ReSharper disable once MethodSupportsCancellation
                        SellAsync(currentCandle).Wait();
                    }
                }
            }
        }

        public async Task BuyAsync(CandleModel candle)
        {
            if (candle != null)
            {
                if (_userBalanceService.HasOpenOrder)
                {
                    return;
                }

                var buyPrice = !_userBalanceService.EnableRealtimeTrading ? candle.ClosePrice : _exchangeProvider.GetTicker(_tradingPair).Result.LowestAsk;
                _userBalanceService.SetBuyPrice(new CandleModel
                {
                    ClosePrice = buyPrice,
                    StartDateTime = DateTime.UtcNow
                });
                if (_userBalanceService.EnableRealtimeTrading)
                {
                    _userBalanceService.OpenOrderNumber = await _exchangeProvider.CreateOrderAsync(TradeType.Buy, _tradingPair, buyPrice, _userBalanceService.Rate);
                    _userBalanceService.HasOpenOrder = true;

                    await CheckOrderInvoked(_userBalanceService.OpenOrderNumber, TradeType.Buy);
                }

                var msg = $"Buy crypto currency. Date: {candle.StartDateTime}; Price: ${buyPrice}; Rate: {_userBalanceService.Rate}; OrderNumber: {_userBalanceService.OpenOrderNumber}\n";
                Console.WriteLine(msg);
                _emailService.SendEmail($"Buying {_tradingPair}", msg);
            }
        }

        public async Task SellAsync(CandleModel candle)
        {
            if (candle != null)
            {
                if (_userBalanceService.HasOpenOrder)
                {
                    await _exchangeProvider.CancelOrderAsync(_userBalanceService.OpenOrderNumber);
                    return;
                }
                var sellPrice = !_userBalanceService.EnableRealtimeTrading ? candle.ClosePrice : _exchangeProvider.GetTicker(_tradingPair).Result.HighestBid;
                _userBalanceService.TradingCount++;
                long orderNumber = 0;
                if (_userBalanceService.EnableRealtimeTrading)
                {
                    orderNumber = await _exchangeProvider.CreateOrderAsync(TradeType.Sell, _tradingPair, sellPrice, _userBalanceService.Rate);
                    _userBalanceService.HasOpenOrder = true;

                    await CheckOrderInvoked(orderNumber, TradeType.Sell);
                }

                var profit = _userBalanceService.GetProfit(sellPrice, candle.StartDateTime);
                var msg = $"Sell crypto currency. Date: {candle.StartDateTime}; Price: ${sellPrice}; Rate: {_userBalanceService.Rate}; OrderNumber: {orderNumber}\n" +
                          $"Profit: ${profit.Profit}\n" +
                          $"Trading time in hours: {Math.Round(profit.TradingTimeInMinutes / 60.0, 2)}\n" +
                          "\n" +
                          _userBalanceService.TradingSummary();

                Console.WriteLine(msg);

                _emailService.SendEmail($"Selling {_tradingPair}", msg);
            }
        }

        private async Task CheckOrderInvoked(long orderNumber, TradeType tradeType)
        {
            await Task.Factory.StartNew(async () =>
            {
                while (_userBalanceService.HasOpenOrder)
                {
                    if (_cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    Thread.Sleep(20000); // sleep 20 seconds

                    var orderDetails = await _exchangeProvider.GetOrderAsync(orderNumber);
                    if (orderDetails != null)
                    {
                        var orderDetail = orderDetails.First();
                        Console.WriteLine($"Open order invoked. OrderNumber: {orderNumber}; OrderDetails: {JsonConvert.SerializeObject(orderDetail)}");
                        if (tradeType == TradeType.Buy)
                        {
                            _userBalanceService.Rate -= Math.Round(_userBalanceService.Rate * orderDetail.Fee, 8);
                            Console.WriteLine($"Real rate: {_userBalanceService.Rate}");
                        }
                        _userBalanceService.HasOpenOrder = false;
                    }
                }
            }, _cancellationToken);
        }

        private long GetSinceUnixTime(CandlePeriod candlePeriod)
        {
            var utcNow = DateTimeOffset.UtcNow;
            var candlePeridInMinutes = (int) candlePeriod;
            var candleSizeInSeconds = candlePeridInMinutes * _strategy.CandleSize * 60;

            return utcNow.AddSeconds(-1 * (candleSizeInSeconds + utcNow.Second)).ToUnixTimeSeconds();
        }
    }
}
