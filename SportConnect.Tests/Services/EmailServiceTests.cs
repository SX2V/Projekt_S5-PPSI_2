using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Moq;
using SportConnect.API.Services;
using Xunit;
using Microsoft.Extensions.Localization;

namespace SportConnect.Tests.Services
{
    public class EmailServiceTests
    {
        private static IConfiguration BuildConfig(string user, string password)
        {
            var settings = new Dictionary<string, string?>
            {
                ["Smtp:Host"] = "smtp.example.test",
                ["Smtp:Port"] = "25",
                ["Smtp:User"] = user,
                ["Smtp:Password"] = password
            };

            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        [Fact]
        public async Task SendEmailAsync_ThrowsWhenUserNotConfigured()
        {
            var config = BuildConfig(user: "", password: "pwd");
            var localizerMock = new Mock<IStringLocalizer<EmailService>>();
            localizerMock.Setup(l => l["SmtpUserNotConfigured"]).Returns(new LocalizedString("SmtpUserNotConfigured", "user not configured"));

            var service = new EmailService(config, localizerMock.Object);

            Func<Task> act = async () => await service.SendEmailAsync("to@example.com", "subj", "body");

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("user not configured");
        }

        [Fact]
        public async Task SendEmailAsync_ThrowsWhenPasswordNotConfigured()
        {
            var config = BuildConfig(user: "user@test", password: "");
            var localizerMock = new Mock<IStringLocalizer<EmailService>>();
            localizerMock.Setup(l => l["SmtpPasswordNotConfigured"]).Returns(new LocalizedString("SmtpPasswordNotConfigured", "password not configured"));

            var service = new EmailService(config, localizerMock.Object);

            Func<Task> act = async () => await service.SendEmailAsync("to@example.com", "subj", "body");

            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("password not configured");
        }

        [Fact]
        public async Task SendEmailAsync_ThrowsWhenRecipientEmpty()
        {
            var config = BuildConfig(user: "user@test", password: "pwd");
            var localizerMock = new Mock<IStringLocalizer<EmailService>>();
            localizerMock.Setup(l => l["RecipientAddressEmpty"]).Returns(new LocalizedString("RecipientAddressEmpty", "recipient is empty"));

            var service = new EmailService(config, localizerMock.Object);

            Func<Task> act = async () => await service.SendEmailAsync("", "subj", "body");

            await act.Should().ThrowAsync<ArgumentException>().WithMessage("*recipient is empty*");
        }
    }
}