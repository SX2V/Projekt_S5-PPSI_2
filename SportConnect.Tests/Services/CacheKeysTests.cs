using System;
using FluentAssertions;
using Xunit;
using SportConnect.API.Services;

namespace SportConnect.Test
{
    public class CacheKeysTests
    {
        [Fact]
        public void UserSports_ReturnsExpectedKey()
        {
            var id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            CacheKeys.UserSports(id).Should().Be($"user:{id}:sports");
        }

        [Fact]
        public void AllSports_ReturnsExpectedKey()
        {
            CacheKeys.AllSports().Should().Be("sports:all");
        }

        [Fact]
        public void UserProfile_ReturnsExpectedKey()
        {
            var id = Guid.NewGuid();
            CacheKeys.UserProfile(id).Should().Be($"user:{id}:profile");
        }
    }
}