using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SportConnect.API.Controllers;
using SportConnect.API.Dtos;
using SportConnect.API.Models;
using SportConnect.API.Services;
using SportConnect.Tests.Helpers;
using Xunit;

namespace SportConnect.Tests.Controller
{
    public class AuthControllerTests
    {
        private readonly Mock<IPasswordHasher<User>> _hasherMock = new();
        private readonly Mock<IConfiguration> _configMock = new();
        private readonly Mock<IActionLogger> _loggerMock = new();
        private readonly Mock<IEmailService> _emailMock = new();
        private readonly Mock<IStringLocalizer<AuthController>> _localizerMock = new();
        private const string JwtKey = "BARDZO_DLUGI_KLUCZ_TESTOWY_32_ZNAKI_MINIMUM_!";

        public AuthControllerTests()
        {
            _configMock.Setup(c => c["Jwt:Key"]).Returns(JwtKey);
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("SportConnect.API");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("SportConnect.Users");
        }

        [Fact]
        public async Task Register_CreatesUser_HashedPassword_And_SendsEmail()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
            _hasherMock.Setup(h => h.HashPassword(It.IsAny<User>(), "pw")).Returns("hashed_pwd");
            _localizerMock.Setup(l => l["UserRegistered"]).Returns(new LocalizedString("UserRegistered", "registered"));

            var controller = new AuthController(context, _hasherMock.Object, _configMock.Object, _loggerMock.Object, _emailMock.Object, _localizerMock.Object);
            var dto = new RegisterDto { Email = "new@user.pl", Password = "pw", Name = "User" };

            var result = await controller.Register(dto);

            result.Should().BeOfType<OkObjectResult>();
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            user.Should().NotBeNull();
            user.PasswordHash.Should().Be("hashed_pwd");
            _emailMock.Verify(e => e.SendEmailAsync(dto.Email, It.IsAny<string>(), It.IsAny<string>()), Times.Once);
            _loggerMock.Verify(a => a.LogAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task Login_ReturnsValidToken_WithCorrectClaims()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Email = "test@test.pl", Name = "T", PasswordHash = "hash", Role = UserRole.Admin };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            _hasherMock.Setup(h => h.VerifyHashedPassword(user, "hash", "pw")).Returns(PasswordVerificationResult.Success);

            var controller = new AuthController(context, _hasherMock.Object, _configMock.Object, _loggerMock.Object, _emailMock.Object, _localizerMock.Object);
            var result = await controller.Login(new LoginDto { Email = user.Email, Password = "pw" });

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var token = (string)okResult.Value.GetType().GetProperty("token").GetValue(okResult.Value, null);

  
            var handler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "SportConnect.API",
                ValidateAudience = true,
                ValidAudience = "SportConnect.Users",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = handler.ValidateToken(token, validationParams, out _);
            principal.Claims.First(c => c.Type == ClaimTypes.Email).Value.Should().Be(user.Email);
            principal.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value.Should().Be(userId.ToString());
            principal.Claims.First(c => c.Type == ClaimTypes.Role).Value.Should().Be("Admin");
        }

        [Fact]
        public async Task Login_WithNonExistingUser_ReturnsUnauthorized()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
            var controller = new AuthController(context, _hasherMock.Object, _configMock.Object, _loggerMock.Object, _emailMock.Object, _localizerMock.Object);

            var result = await controller.Login(new LoginDto { Email = "none@test.pl", Password = "any" });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_WithWrongPassword_ReturnsUnauthorized()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
        
            var user = new User { Name = "Test User", Email = "u@t.pl", PasswordHash = "hash" };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            _hasherMock.Setup(h => h.VerifyHashedPassword(user, "hash", "wrong")).Returns(PasswordVerificationResult.Failed);
            _localizerMock.Setup(l => l["InvalidCredentials"]).Returns(new LocalizedString("InvalidCredentials", "err"));

            var controller = new AuthController(context, _hasherMock.Object, _configMock.Object, _loggerMock.Object, _emailMock.Object, _localizerMock.Object);
            var result = await controller.Login(new LoginDto { Email = "u@t.pl", Password = "wrong" });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public async Task Login_ShouldReturnUnauthorized_WhenUserIsBlocked()
        {
            using var context = TestDbContextFactory.CreateInMemoryContext();
  
            var user = new User { Name = "Blocked User", Email = "b@t.pl", PasswordHash = "h", IsBlocked = true };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            _hasherMock.Setup(h => h.VerifyHashedPassword(user, "h", "pw")).Returns(PasswordVerificationResult.Success);
            _localizerMock.Setup(l => l["UserBlocked"]).Returns(new LocalizedString("UserBlocked", "blocked"));

            var controller = new AuthController(context, _hasherMock.Object, _configMock.Object, _loggerMock.Object, _emailMock.Object, _localizerMock.Object);
            var result = await controller.Login(new LoginDto { Email = "b@t.pl", Password = "pw" });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }
    }
}