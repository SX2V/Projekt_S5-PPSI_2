using System;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using SportConnect.API.Controllers;
using SportConnect.API.Dtos;
using SportConnect.API.Models;
using SportConnect.Tests.Helpers;
using Xunit;

namespace SportConnect.Tests.Controller
{
    public class TrainingRequestsControllerTests
    {
        private static ClaimsPrincipal BuildUserPrincipal(Guid userId)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }, "TestAuth");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task Create_Succeeds_WhenDataIsCorrect()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
            var senderId = Guid.NewGuid();
            var receiverId = Guid.NewGuid();
            context.Users.Add(new User { Id = receiverId, Email = "r@t.pl", Name = "R" });
            await context.SaveChangesAsync();

            var controller = new TrainingRequestsController(context, new Mock<IStringLocalizer<TrainingRequestsController>>().Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = BuildUserPrincipal(senderId) } };

            var dto = new CreateTrainingRequestDto
            {
                ReceiverId = receiverId,
                TrainingDateTime = DateTime.UtcNow.AddDays(1),
                Location = "Gym",
                Message = "Hi"
            };

            var result = await controller.Create(dto);

            result.Should().BeOfType<CreatedAtActionResult>();
            var created = await context.TrainingRequests.AnyAsync(r => r.SenderId == senderId && r.ReceiverId == receiverId);
            created.Should().BeTrue();
        }


        [Fact]
        public async Task Create_ReturnsBadRequest_WhenSendingToYourself()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var controller = new TrainingRequestsController(context, new Mock<IStringLocalizer<TrainingRequestsController>>().Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = BuildUserPrincipal(userId) } };

            var dto = new CreateTrainingRequestDto { ReceiverId = userId, TrainingDateTime = DateTime.UtcNow.AddDays(1) };

            var result = await controller.Create(dto);

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ReturnsNotFound_WhenReceiverDoesNotExist()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new TrainingRequestsController(context, new Mock<IStringLocalizer<TrainingRequestsController>>().Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = BuildUserPrincipal(Guid.NewGuid()) } };

            var dto = new CreateTrainingRequestDto { ReceiverId = Guid.NewGuid(), TrainingDateTime = DateTime.UtcNow.AddDays(1) };

            var result = await controller.Create(dto);

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}