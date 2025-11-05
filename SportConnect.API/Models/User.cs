using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Models
{
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
        public ICollection<UserSport> UserSports { get; set; } = new List<UserSport>();

    }
}
