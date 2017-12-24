using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using CryptoTrading.Logic;
using CryptoTrading.Logic.Repositories.Interfaces;
using CryptoTrading.Logic.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using TradingTester.AutoMapper;

namespace TradingTester
{
    public class Program
    {
        public static void InitializeAutoMappers()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
                cfg.AddProfile<CryptoTrading.Logic.AutoMapper.AutoMapperProfile>();
            });
        }

        public static void Main(string[] args)
        {
            var options = new ArgumentOptions();
            var isValid = CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
            if (!isValid)
            {
                Console.WriteLine("Parameter is wrong!");
                return;
            }
            
            InitializeAutoMappers();
            var serviceProvider = InitDependencyInjection.Init(options.Exchange, options.Strategy, options.EnableBacktest);

            if (options.EnableBacktest)
            {
                var candleService = serviceProvider.GetService<ICandleService>();
                var availableCandlePeriods = candleService.GetAvailableCandlePeriodsAsync(options.TradingPair).Result.ToList();

                Console.WriteLine("Available candle preiods:");
                var periodOptions = new Dictionary<int, long>();
                for (int i = 0; i < availableCandlePeriods.Count; i++)
                {
                    var availableCandlePeriod = availableCandlePeriods[i];
                    Console.WriteLine($"Options {i + 1}: {availableCandlePeriod.PeriodStart:yyyy-MM-dd hh:mm} - {availableCandlePeriod.PeriodEnd:yyyy-MM-dd hh:mm}; Sample count: {availableCandlePeriod.Candles.Count()}");
                    Console.WriteLine();
                    periodOptions.Add(i + 1, availableCandlePeriod.ScanId);
                }
                Console.WriteLine("Select option: ");
                var key = Console.ReadKey();

                int selectedOption;
                if (!int.TryParse(key.KeyChar.ToString(), out selectedOption))
                {
                    Console.WriteLine("Not exists option!");
                    return;
                }
                var scanId = periodOptions[selectedOption];
                var candles = availableCandlePeriods.First(w => w.ScanId == scanId).Candles.ToList();
                Console.WriteLine();
                Console.WriteLine($"Selected option: {selectedOption}; Candle count: {candles.Count}");

                var backtesTraderService = serviceProvider.GetService<ITraderService>();
                backtesTraderService.CheckStrategyAsync(candles);
                
                Console.WriteLine("############ SUMMARY ############");
                var userBalanceService = serviceProvider.GetService<IUserBalanceService>();
                Console.WriteLine(userBalanceService.TradingSummary());
            }

            if (options.EnableImport)
            {
                var importProvider = serviceProvider.GetService<IImportRepository>();
                var candles = importProvider.ImportCandlesAsync(options.TradingPair, options.ImportIntervalInHour, options.CandlePeriod).Result;
                Console.WriteLine($"Imported candle count: {candles.Count()}");
            }

            if (options.EnableOrderTesting)
            {
                //var exchangeProvider = serviceProvider.GetService<IExchangeProvider>();

                //decimal price = 19150;
                //decimal defaultAmount = 10;
                ////var orderBook = exchangeProvider.GetOrderBook(options.TradingPair, 1).Result;
                ////price = orderBook.SellOrders.First();
                //var ticker = exchangeProvider.GetTicker(options.TradingPair).Result;
                //price = ticker.HighestBid;
                ////var balance = exchangeProvider.GetBalanceAsync().Result;
                //var rate = Math.Round(defaultAmount / price, 8);
                ////var orderNumber = exchangeProvider.CreateOrderAsync(TradeType.Buy, options.TradingPair, price, rate).Result;
                ////var result = exchangeProvider.CancelOrderAsync(orderNumber).Result;
                ////ticker = exchangeProvider.GetTicker(options.TradingPair).Result;
                ////price = ticker.HighestBid;
                //var orderNumber2 = exchangeProvider.CreateOrderAsync(TradeType.Sell, options.TradingPair, price, (decimal)0.00057228).Result;
                ////var a = exchangeProvider.GetOrderBook(options.TradingPair, 1).Result;
            }
        }
    }
}
