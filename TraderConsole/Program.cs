using System;
using System.Threading;
using AutoMapper;
using CryptoTrading.Logic;
using CryptoTrading.Logic.AutoMapper;
using CryptoTrading.Logic.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace TraderConsole
{
    public class Program
    {
        public static void InitializeAutoMappers()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<AutoMapperProfile>();
            });
        }

        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        private static ITraderService _realTimeService;
        private static IUserBalanceService _userBalanceService;

        // ReSharper disable once UnusedParameter.Local
        public static void Main(string[] args)
        {
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
                CancellationTokenSource.Cancel();
                Console.WriteLine("Waiting....");
            };

            var options = new ArgumentOptions();
            var isValid = CommandLine.Parser.Default.ParseArgumentsStrict(args, options);
            if (!isValid)
            {
                Console.WriteLine("Parameter is wrong!");
                return;
            }

            InitializeAutoMappers();
            var serviceProvider = InitDependencyInjection.Init(options.Exchange, options.Strategy);

            Console.WriteLine("Starting parameters:");
            Console.WriteLine($"\tExchange name: {options.Exchange}");
            Console.WriteLine($"\tTrading pair: {options.TradingPair}");
            Console.WriteLine($"\tCandle period: {options.CandlePeriod}");
            Console.WriteLine($"\tStrategy name: {options.Strategy}");
            Console.WriteLine();

            try
            {
                _userBalanceService = serviceProvider.GetService<IUserBalanceService>();
                _realTimeService = serviceProvider.GetService<ITraderService>();

                var utcNow = DateTime.UtcNow;
                var delayStartInSeconds = 60 - utcNow.Second;
                Console.WriteLine($"Delaying realtime trading start. Delay time (seconds): {delayStartInSeconds}");

                Thread.Sleep(delayStartInSeconds * 1000);

                Console.WriteLine();
                Console.WriteLine("Started trading");
                Console.WriteLine();

                _realTimeService.StartTradingAsync(options.TradingPair, options.CandlePeriod, CancellationTokenSource.Token).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
            }

            Console.WriteLine("############ SUMMARY ############");
            Console.WriteLine(_userBalanceService.TradingSummary());
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
