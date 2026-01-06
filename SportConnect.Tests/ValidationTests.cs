using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using SportConnect.API.Models;
using Xunit;

namespace SportConnect.Tests
{
    public class ValidationTests
    {
        private static IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, ctx, validationResults, true);
            return validationResults;
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(101)]
        [InlineData(150)]
        public void User_SearchRadiusKm_MustBeInRange(int radius)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                SearchRadiusKm = radius,
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("SearchRadiusKm"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(50)]
        [InlineData(100)]
        public void User_SearchRadiusKm_AcceptsValidRange(int radius)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                SearchRadiusKm = radius,
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().NotContain(r => r.MemberNames.Contains("SearchRadiusKm"));
        }

        [Theory]
        [InlineData(-91)]
        [InlineData(91)]
        [InlineData(-100)]
        [InlineData(100)]
        public void User_Latitude_MustBeInRange(double lat)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                Latitude = lat,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Latitude"));
        }

        [Theory]
        [InlineData(-90)]
        [InlineData(0)]
        [InlineData(90)]
        public void User_Latitude_AcceptsValidRange(double lat)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                Latitude = lat,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().NotContain(r => r.MemberNames.Contains("Latitude"));
        }

        [Theory]
        [InlineData(-181)]
        [InlineData(181)]
        [InlineData(-200)]
        [InlineData(200)]
        public void User_Longitude_MustBeInRange(double lon)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                Latitude = 50.0,
                Longitude = lon
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Longitude"));
        }

        [Theory]
        [InlineData(-180)]
        [InlineData(0)]
        [InlineData(180)]
        public void User_Longitude_AcceptsValidRange(double lon)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                Latitude = 50.0,
                Longitude = lon
            };

            var results = ValidateModel(user);

            results.Should().NotContain(r => r.MemberNames.Contains("Longitude"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(121)]
        [InlineData(200)]
        public void User_Age_MustBeInRange(int age)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                Age = age,
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Age"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(25)]
        [InlineData(120)]
        public void User_Age_AcceptsValidRange(int age)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                Age = age,
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().NotContain(r => r.MemberNames.Contains("Age"));
        }

        [Fact]
        public void User_Name_MustNotExceed50Chars()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = new string('a', 51),
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Name"));
        }

        [Fact]
        public void User_Description_MustNotExceed250Chars()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "Test",
                Description = new string('a', 251),
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Description"));
        }

        [Fact]
        public void Sport_Name_MustNotExceed100Chars()
        {
            var sport = new Sport
            {
                Id = Guid.NewGuid(),
                Name = new string('a', 101)
            };

            var results = ValidateModel(sport);

            results.Should().Contain(r => r.MemberNames.Contains("Name"));
        }

        [Fact]
        public void Sport_Name_CannotBeEmpty()
        {
            var sport = new Sport
            {
                Id = Guid.NewGuid(),
                Name = ""
            };

            var results = ValidateModel(sport);

            results.Should().Contain(r => r.MemberNames.Contains("Name"));
        }

        [Fact]
        public void Sport_Description_MustNotExceed250Chars()
        {
            var sport = new Sport
            {
                Id = Guid.NewGuid(),
                Name = "Tennis",
                Description = new string('a', 251)
            };

            var results = ValidateModel(sport);

            results.Should().Contain(r => r.MemberNames.Contains("Description"));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(1001)]
        public void Sport_TypicalDistanceKm_MustBeInRange(int distance)
        {
            var sport = new Sport
            {
                Id = Guid.NewGuid(),
                Name = "Tennis",
                TypicalDistanceKm = distance
            };

            var results = ValidateModel(sport);

            results.Should().Contain(r => r.MemberNames.Contains("TypicalDistanceKm"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(500)]
        [InlineData(1000)]
        public void Sport_TypicalDistanceKm_AcceptsValidRange(int distance)
        {
            var sport = new Sport
            {
                Id = Guid.NewGuid(),
                Name = "Tennis",
                TypicalDistanceKm = distance
            };

            var results = ValidateModel(sport);

            results.Should().NotContain(r => r.MemberNames.Contains("TypicalDistanceKm"));
        }

        [Fact]
        public void User_Email_MustBeValidFormat()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "invalid-email",
                Name = "Test",
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Email"));
        }

        [Theory]
        [InlineData("test@test.com")]
        [InlineData("user.name@example.co.uk")]
        [InlineData("first+last@domain.com")]
        public void User_Email_AcceptsValidFormats(string email)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                Name = "Test",
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().NotContain(r => r.MemberNames.Contains("Email"));
        }

        [Fact]
        public void User_Name_CannotBeEmpty()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@test.com",
                Name = "",
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Name"));
        }

        [Fact]
        public void User_Email_CannotBeEmpty()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "",
                Name = "Test",
                Latitude = 50.0,
                Longitude = 20.0
            };

            var results = ValidateModel(user);

            results.Should().Contain(r => r.MemberNames.Contains("Email"));
        }
    }
}