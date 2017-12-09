using System.Net;
using System.Net.Mail;
using CryptoTrading.Logic.Options;
using CryptoTrading.Logic.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace CryptoTrading.Logic.Services
{
    public class EmailService : IEmailService
    {
        private readonly SmtpClient _smtpClient;
        private readonly MailAddress _fromAddress;
        private readonly MailAddress _toAddress;
        private readonly EmailOptions _emailOptions;

        public EmailService(IOptions<EmailOptions> emailOptions)
        {
            _emailOptions = emailOptions.Value;

            _fromAddress = new MailAddress(_emailOptions.FromAddress, _emailOptions.FromName);
            _toAddress = new MailAddress(_emailOptions.ToAddress, _emailOptions.ToName);

            _smtpClient = new SmtpClient
            {
                Host = _emailOptions.SmtpHost,
                Port = _emailOptions.SmtpPort,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_fromAddress.Address, _emailOptions.FromPassword)
            };
        }

        public void SendEmail(string subject, string message)
        {
            if (!_emailOptions.Enable)
            {
                return;
            }

            using (var mailMsg = new MailMessage(_fromAddress, _toAddress)
            {
                Subject = subject,
                Body = message
            })
            {
                _smtpClient.Send(mailMsg);
            }
        }
    }
}
