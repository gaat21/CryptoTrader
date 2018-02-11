using System.Collections.Generic;

namespace CryptoTrading.Logic.Models
{
    public class IchimokuCloudModel
    {
        public decimal TenkanSenValue { get; set; }

        public decimal KijunSenValue { get; set; }

        public decimal SenkouSpanAValue { get; set; }

        public decimal SenkouSpanBValue { get; set; }

        public List<decimal> SsaFuture { get; set; }

        public List<decimal> SsbFuture { get; set; }

        public bool SsaCrossoverSsb { get; set; }
    }
}