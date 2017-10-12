using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingTester.AutoMapper;
using TradingTester.DAL;
using TradingTester.Logic.Indicators;
using TradingTester.Logic.Indicators.Interfaces;
using TradingTester.Logic.Options;
using TradingTester.Logic.Providers;
using TradingTester.Logic.Providers.Interfaces;
using TradingTester.Logic.Repositories;
using TradingTester.Logic.Repositories.Interfaces;
using TradingTester.Logic.Services;
using TradingTester.Logic.Services.Interfaces;
using TradingTester.Logic.Strategies;
using TradingTester.Logic.Strategies.Interfaces;
using TradingTester.Options;

namespace TradingTester
{
    public class Program
    {
        private static IServiceProvider Init(ArgumentOptions options)
        {
            var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Configs/config.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();

            switch (options.Exchange)
            {
                case ExhangeEnum.Kraken:
                    serviceCollection.Configure<KrakenOptions>(configuration.GetSection("Kraken"));
                    serviceCollection.AddTransient<IExchangeProvider, KrakenExchangeProvider>();
                    break;
                case ExhangeEnum.Poloniex:
                    serviceCollection.Configure<PoloniexOptions>(configuration.GetSection("Poloniex"));
                    serviceCollection.AddTransient<IExchangeProvider, PoloniexExchangeProvider>();
                    break;
            }

            serviceCollection.Configure<EmaStrategyOptions>(configuration.GetSection("EmaStrategy"));
            serviceCollection.AddMemoryCache();
            serviceCollection.AddDbContext<TradingDbContext>(
                opt => opt.UseMySql(configuration.GetSection("Database")?.Get<DatabaseOptions>()?.ConnectionString)
            );

            serviceCollection.AddSingleton<IUserBalanceService, UserBalanceService>();
            serviceCollection.AddTransient<ITradingDbContext, TradingDbContext>();
            serviceCollection.AddTransient<ICandleRepository, CandleDbRepository>();
            serviceCollection.AddTransient<IImportRepository, ImportRepository>();
            serviceCollection.AddTransient<ITraderService, BacktestTraderService>();
            serviceCollection.AddTransient<ICandleService, CandleDbService>();
            serviceCollection.AddTransient<IIndicatorFactory, EmaIndicatorFactory>();
            serviceCollection.AddTransient<IIndicator, EmaIndicator>();
            //serviceCollection.AddTransient<IStrategy, EmaStrategy>();
            serviceCollection.AddTransient<IStrategy, CustomStrategy>();

            return serviceCollection.BuildServiceProvider();
        }

        public static void InitializeAutoMappers()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
                cfg.AddProfile<Logic.AutoMapper.AutoMapperProfile>();
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
            var serviceProvider = Init(options);

            if (options.EnableBacktest)
            {
                var candleService = serviceProvider.GetService<ICandleService>();
                var availableCandlePeriods = candleService.GetAvailableCandlePeriodsAsync(options.TradingPair).Result.ToList();

                Console.WriteLine("Available candle preiods:");
                var periodOptions = new Dictionary<int, long>();
                for (int i = 0; i < availableCandlePeriods.Count; i++)
                {
                    var availableCandlePeriod = availableCandlePeriods[i];
                    Console.WriteLine($"Options {i + 1}: {availableCandlePeriod.PeriodStart:yyyy-MM-dd hh:mm} - {availableCandlePeriod.PeriodEnd:yyyy-MM-dd hh:mm}");
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
                if (!periodOptions.ContainsKey(selectedOption))
                {
                }
                var scanId = periodOptions[selectedOption];
                var candles = availableCandlePeriods.First(w => w.ScanId == scanId).Candles.ToList();
                Console.WriteLine();
                Console.WriteLine($"Selected option: {selectedOption}; Candle count: {candles.Count}");

                var backtesTraderService = serviceProvider.GetService<ITraderService>();
                foreach (var candle in candles)
                {
                    backtesTraderService.CheckStrategyAsync(candle);
                }
                Console.WriteLine("############ SUMMARY ############");
                Console.WriteLine($"Trading count: {backtesTraderService.TradingCount}");
                var userBalanceService = serviceProvider.GetService<IUserBalanceService>();
                Console.WriteLine($"Total profit: ${userBalanceService.TotalProfit}");
                Console.WriteLine($"Total profit %: {decimal.Round(userBalanceService.TotalPercentage, 2)}%");
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
