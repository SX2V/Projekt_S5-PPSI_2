using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class UpdateProfileDto
    {
        [MaxLength(50, ErrorMessage = "The maximum length for 'name' is 50 characters.")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        public string? Email { get; set; }

        [Range(0, 120, ErrorMessage = "Age must be between 0 and 120.")]
        public int? Age { get; set; }

        [MaxLength(250, ErrorMessage = "The maximum length for 'description' is 250 characters.")]
        public string? Description { get; set; }
    }
}
