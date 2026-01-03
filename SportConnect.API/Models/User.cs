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

        [Required(ErrorMessage = "The 'name' field is required.")]
        [MaxLength(50, ErrorMessage = "The maximum length for 'name' is 50 characters.")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "The 'email' field is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; }

        public bool IsAvailableNow { get; set; } = false;

        [Range(0, 100, ErrorMessage = "Search radius must be between 0 and 100 kilometers.")]
        public int SearchRadiusKm { get; set; } = 0;

        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
        public int? Age { get; set; }

        [MaxLength(250, ErrorMessage = "The maximum length for 'description' is 250 characters.")]
        public string? Description { get; set; }

        public ICollection<UserSport> UserSports { get; set; } = new List<UserSport>();

        [JsonIgnore]
        public string PasswordHash { get; set; } = string.Empty;

        [Column(TypeName = "varchar(20)")]
        public UserRole Role { get; set; } = UserRole.User;

        public bool IsBlocked { get; set; } = false;

        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; } = 0;

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; } = 0;

        // Zmiany
        
        // Facebook User ID - używane do logowania przez Facebook
        
        [MaxLength(100)]
        public string? FacebookId { get; set; }
        
        // Strava Athlete ID - używane do logowania przez Strava

        public long? StravaId { get; set; }
        
        // Data utworzenia konta użytkownika
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Data ostatniej aktualizacji profilu użytkownika

        public DateTime? UpdatedAt { get; set; }
    }
}
