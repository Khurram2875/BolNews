using BolNews.Application.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace BolNews.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly EmailOptions _options;

        public SmtpEmailService(
            IOptions<EmailOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendAsync(
            string toEmail,
            string subject,
            string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_options.SmtpHost))
            {
                throw new InvalidOperationException(
                    "SMTP host is not configured.");
            }

            if (string.IsNullOrWhiteSpace(_options.FromEmail))
            {
                throw new InvalidOperationException(
                    "Email sender address is not configured.");
            }

            using var message = new MailMessage
            {
                From = new MailAddress(
                    _options.FromEmail,
                    _options.FromName),

                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(
                _options.SmtpHost,
                _options.SmtpPort)
            {
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    _options.SmtpUsername,
                    _options.SmtpPassword)
            };

            await client.SendMailAsync(message);
        }
    }
}
