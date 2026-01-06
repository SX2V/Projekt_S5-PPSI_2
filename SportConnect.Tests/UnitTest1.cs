using Microsoft.AspNetCore.Identity;
using SportConnect.API.Models; 
using Xunit;

namespace SportConnect.Tests
{
    public class PasswordTests
    {
        [Fact]
        public void PasswordHasher_ShouldHashCorrectly()
        {
            var hasher = new PasswordHasher<User>();
            var user = new User { Email = "test@test.com" };
            var password = "TajneHaslo123!";

            var hash = hasher.HashPassword(user, password);
            var result = hasher.VerifyHashedPassword(user, hash, password);

            Assert.Equal(PasswordVerificationResult.Success, result);
        }
    }
}