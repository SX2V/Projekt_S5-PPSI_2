using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Localization;

namespace SportConnect.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly IStringLocalizer<EmailService> _localizer;

        public EmailService(IConfiguration config, IStringLocalizer<EmailService> localizer)
        {
            _config = config;
            _localizer = localizer;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"]!);
            var user = smtpSection["User"];
            var password = smtpSection["Password"];

            if (string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException(_localizer["SmtpUserNotConfigured"]);

            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(_localizer["SmtpPasswordNotConfigured"]);

            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException(_localizer["RecipientAddressEmpty"], nameof(to));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SportConnect", user));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject ?? string.Empty;
            message.Body = new TextPart("plain") { Text = body ?? string.Empty };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
