using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using SportConnect.API.Services;
using SportConnect.Tests.Helpers;
using Xunit;

namespace SportConnect.Tests
{
    public class ActionLoggerTests
    {
        [Fact]
        public async Task LogAsync_AddsActionLogToDbContext()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
            var logger = new ActionLogger(context);

            var userId = System.Guid.NewGuid();
            var action = "test action";

            await logger.LogAsync(userId, action);

            var logs = context.ActionLogs.ToList();
            logs.Should().HaveCount(1);
            var log = logs[0];
            log.UserId.Should().Be(userId);
            log.Action.Should().Be(action);
        }
    }
}