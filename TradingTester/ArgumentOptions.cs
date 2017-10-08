using CommandLine;

namespace TradingTester
{
    public class ArgumentOptions
    {
        [Option('e', "exchange", Required = true, HelpText = "Exchange. Value: Kraken, Poloniex")]
        public ExhangeEnum Exchange { get; set; }

        [Option('t', "tradingpair", Required = true, HelpText = "Trading pair")]
        public string TradingPair { get; set; }

        [Option('b', "backtest", HelpText = "Backtesting imported data")]
        public bool EnableBacktest { get; set; }

        [Option('i', "import", HelpText = "Imported data")]
        public bool EnableImport { get; set; }
    }
}
