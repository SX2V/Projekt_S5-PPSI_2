using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class UpdateProfileDto
    {
        [MaxLength(50, ErrorMessage = "UpdateProfileNameMaxLength")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "UpdateProfileEmailInvalid")]
        public string? Email { get; set; }
        [Range(0, 120, ErrorMessage = "UpdateProfileAgeRange")]
        public int? Age { get; set; }

        [MaxLength(250, ErrorMessage = "UpdateProfileDescriptionMaxLength")]
        public string? Description { get; set; }
    }
}
