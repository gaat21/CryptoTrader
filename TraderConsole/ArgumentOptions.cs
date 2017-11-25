using CommandLine;
using CryptoTrading.Logic.Models;
using CryptoTrading.Logic.Providers.Models;

namespace TraderConsole
{
    public class ArgumentOptions
    {
        [Option('e', "exchange", Required = true, HelpText = "Exchange. Value: Kraken, Poloniex")]
        public ExchangeEnum Exchange { get; set; }

        [Option('t', "tradingpair", Required = true, HelpText = "Trading pair")]
        public string TradingPair { get; set; }

        [Option('p', "candle-period", HelpText = "Candle period")]
        public CandlePeriod CandlePeriod { get; set; } = CandlePeriod.OneMinute;

        [Option('s', "strategy", HelpText = "Trading strategy")]
        public Strategy Strategy { get; set; } = Strategy.Custom;
    }
}
