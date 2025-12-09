using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SportConnect.API.Models
{
    public enum UserRole { User, Admin }
    public class User
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "UserNameRequired")]
        [MaxLength(50, ErrorMessage = "UserNameMaxLength")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "UserEmailRequired")]
        [EmailAddress(ErrorMessage = "UserEmailInvalid")]
        public string? Email { get; set; }

        public bool IsAvailableNow { get; set; } = false;

        [Range(0, 100, ErrorMessage = "UserSearchRadiusRange")]
        public int SearchRadiusKm { get; set; } = 0;

        [Range(0, 120, ErrorMessage = "UserAgeRange")]
        public int? Age { get; set; }

        [MaxLength(250, ErrorMessage = "UserDescriptionMaxLength")]
        public string? Description { get; set; }

        public ICollection<UserSport> UserSports { get; set; } = new List<UserSport>();

        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [Column(TypeName = "varchar(20)")]
        public UserRole Role { get; set; } = UserRole.User;

        public bool IsBlocked { get; set; } = false;

        [Range(-90, 90, ErrorMessage = "UserLatitudeRange")]
        public double Latitude { get; set; } = 0;

        [Range(-180, 180, ErrorMessage = "UserLongitudeRange")]
        public double Longitude { get; set; } = 0;
        public string? ProfilePicturePath { get; set; }
    }
}
