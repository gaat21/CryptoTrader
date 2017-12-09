namespace CryptoTrading.Logic.Options
{
    public class EmailOptions
    {
        public bool Enable { get; set; }

        public string SmtpHost { get; set; } = "smtp.gmail.com";

        public int SmtpPort { get; set; } = 587;

        public string FromAddress { get; set; }

        public string FromName { get; set; }

        public string ToAddress { get; set; }

        public string ToName { get; set; }

        public string FromPassword { get; set; }
    }
}
