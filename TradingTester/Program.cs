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
                Console.WriteLine($"Trading count: {backtesTraderService.TradingCount}");
                var userBalanceService = serviceProvider.GetService<IUserBalanceService>();
                Console.WriteLine($"Total profit: ${userBalanceService.TotalProfit}");
                Console.WriteLine($"Total profit %: {decimal.Round(userBalanceService.TotalProfitPercentage, 2)}%");
                Console.WriteLine();
                Console.WriteLine($"Total normal profit: ${userBalanceService.TotalNormalProfit}");
                Console.WriteLine($"Total normal profit %: {decimal.Round(userBalanceService.TotalNormalProfitPercentage, 2)}%");
            }

            if (options.EnableImport)
            {
                var importProvider = serviceProvider.GetService<IImportRepository>();
                var candles = importProvider.ImportCandlesAsync(options.TradingPair, options.ImportIntervalInHour, options.CandlePeriod).Result;
                Console.WriteLine($"Imported candle count: {candles.Count()}");
            }
        }
    }
}
