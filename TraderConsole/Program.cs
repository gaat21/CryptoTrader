using System;
using System.IO;
using System.Threading;
using AutoMapper;
using CryptoTrading.DAL;
using CryptoTrading.Logic.AutoMapper;
using CryptoTrading.Logic.Indicators;
using CryptoTrading.Logic.Indicators.Interfaces;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Providers;
using CryptoTrading.Logic.Providers.Interfaces;
using CryptoTrading.Logic.Providers.Models;
using CryptoTrading.Logic.Repositories;
using CryptoTrading.Logic.Repositories.Interfaces;
using CryptoTrading.Logic.Services;
using CryptoTrading.Logic.Services.Interfaces;
using CryptoTrading.Logic.Strategies;
using CryptoTrading.Logic.Strategies.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingTester.Options;

namespace TraderConsole
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
                // ReSharper disable once AssignNullToNotNullAttribute
                opt => opt.UseMySql(configuration.GetSection("Database")?.Get<DatabaseOptions>()?.ConnectionString)
            );

            serviceCollection.AddSingleton<IUserBalanceService, UserBalanceService>();
            serviceCollection.AddTransient<ITradingDbContext, TradingDbContext>();
            serviceCollection.AddTransient<ICandleRepository, CandleDbRepository>();
            serviceCollection.AddTransient<IImportRepository, ImportRepository>();
            serviceCollection.AddTransient<ITraderService, RealTimeTraderService>();
            serviceCollection.AddTransient<ICandleService, CandleDbService>();
            serviceCollection.AddTransient<IIndicatorFactory, EmaIndicatorFactory>();
            serviceCollection.AddTransient<IIndicator, EmaIndicator>();

            switch (options.Strategy)
            {
                case Strategy.Ema:
                    serviceCollection.AddTransient<IStrategy, EmaStrategy>();
                    break;
                case Strategy.Custom:
                    serviceCollection.AddTransient<IStrategy, CustomStrategy>();
                    break;
            }

            return serviceCollection.BuildServiceProvider();
        }

        public static void InitializeAutoMappers()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });
        }

        // ReSharper disable once UnusedParameter.Local
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

            try
            {
                var realTimeService = serviceProvider.GetService<ITraderService>();
                var cancellationToken = new CancellationToken();
                realTimeService.StartTradingAsync(options.TradingPair, options.CandlePeriod, cancellationToken).Wait(cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }

            //Console.WriteLine("############ SUMMARY ############");
            //Console.WriteLine($"Trading count: {realTimeService.TradingCount}");
            //var userBalanceService = serviceProvider.GetService<IUserBalanceService>();
            //Console.WriteLine($"Total profit: ${userBalanceService.TotalProfit}");
            //Console.WriteLine($"Total profit %: {decimal.Round(userBalanceService.TotalPercentage, 2)}%");

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
