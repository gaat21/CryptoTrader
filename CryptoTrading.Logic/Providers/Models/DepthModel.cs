using System;
using System.Collections.Generic;

namespace CryptoTrading.Logic.Providers.Models
{
    public class DepthModel
    {
        public List<PriceRateModel> Bids { get; set; }

        public List<PriceRateModel> Asks { get; set; }
    }

    public class PriceRateModel
    {
        public decimal Price { get; set; }

        public decimal Quantity { get; set; }

        public override string ToString()
        {
            return $"${Math.Round(Price, 4)}/{Math.Round(Quantity, 4)}";
        }
    }
}