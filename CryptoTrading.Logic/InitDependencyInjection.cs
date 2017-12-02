using System;
using System.IO;
using CryptoTrading.DAL;
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

namespace CryptoTrading.Logic
{
    public static class InitDependencyInjection
    {
        public static IServiceProvider Init(ExchangeEnum exchange, Strategy strategy, bool isBacktest = false)
        {
            var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("Configs/config.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddOptions();

            switch (exchange)
            {
                case ExchangeEnum.Kraken:
                    serviceCollection.Configure<KrakenOptions>(configuration.GetSection("Kraken"));
                    serviceCollection.AddTransient<IExchangeProvider, KrakenExchangeProvider>();
                    break;
                case ExchangeEnum.Poloniex:
                    serviceCollection.Configure<PoloniexOptions>(configuration.GetSection("Poloniex"));
                    serviceCollection.AddTransient<IExchangeProvider, PoloniexExchangeProvider>();
                    break;
                case ExchangeEnum.Bitfinex:
                    serviceCollection.Configure<BitfinexOptions>(configuration.GetSection("Bitfinex"));
                    serviceCollection.AddTransient<IExchangeProvider, BitfinexExchangeProvider>();
                    break;
            }

            serviceCollection.Configure<EmaStrategyOptions>(configuration.GetSection("EmaStrategy"));
            serviceCollection.Configure<MfiStrategyOptions>(configuration.GetSection("MfiStrategy"));
            serviceCollection.Configure<MacdStrategyOptions>(configuration.GetSection("MacdStrategy"));
            serviceCollection.AddMemoryCache();
            serviceCollection.AddDbContext<TradingDbContext>(
                // ReSharper disable once AssignNullToNotNullAttribute
                opt => opt.UseMySql(configuration.GetSection("Database")?.Get<DatabaseOptions>()?.ConnectionString)
            );

            serviceCollection.AddSingleton<IUserBalanceService, UserBalanceService>();
            serviceCollection.AddTransient<ITradingDbContext, TradingDbContext>();
            serviceCollection.AddTransient<ICandleRepository, CandleDbRepository>();
            serviceCollection.AddTransient<IImportRepository, ImportRepository>();

            if (isBacktest)
            {
                serviceCollection.AddTransient<ITraderService, BacktestTraderService>();
            }
            else
            {
                serviceCollection.AddTransient<ITraderService, RealTimeTraderService>();
            }
            serviceCollection.AddTransient<ICandleService, CandleDbService>();
            serviceCollection.AddTransient<IIndicatorFactory, IndicatorFactory>();
            serviceCollection.AddTransient<IIndicator, EmaIndicator>();
            serviceCollection.AddTransient<IIndicator, TsiIndicator>();
            serviceCollection.AddTransient<IIndicator, RsiIndicator>();
            serviceCollection.AddTransient<IIndicator, CandleSticksIndicator>();

            switch (strategy)
            {
                case Strategy.Ema:
                    serviceCollection.AddTransient<IStrategy, EmaStrategy>();
                    break;
                case Strategy.Rsi:
                    serviceCollection.AddTransient<IStrategy, RsiStrategy>();
                    break;
                case Strategy.Macd:
                    serviceCollection.AddTransient<IStrategy, MacdStrategy>();
                    break;
                case Strategy.Mfi:
                    serviceCollection.AddTransient<IStrategy, MfiStrategy>();
                    break;
                case Strategy.CandleSticks:
                    serviceCollection.AddTransient<IStrategy, CandleSticksStrategy>();
                    break;
                case Strategy.Custom:
                    serviceCollection.AddTransient<IStrategy, CustomStrategy>();
                    break;
            }

            return serviceCollection.BuildServiceProvider();
        }
    }
}
