using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SportConnect.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host = smtpSection["Host"];
            var port = int.Parse(smtpSection["Port"]!);
            var user = smtpSection["User"];
            var password = smtpSection["Password"];

            if (string.IsNullOrWhiteSpace(user))
                throw new InvalidOperationException("SMTP User (nadawca) nie jest skonfigurowany.");

            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Adres odbiorcy nie może być pusty.", nameof(to));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("SportConnect", user));
            message.To.Add(new MailboxAddress("", to));
            message.Subject = subject ?? string.Empty;
            message.Body = new TextPart("plain") { Text = body ?? string.Empty };

            using var client = new MailKit.Net.Smtp.SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(user, password); // hasło aplikacyjne
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
