namespace CryptoTrading.Logic.Services.Interfaces
{
    public interface IEmailService
    {
        void SendEmail(string subject, string message);
    }
}