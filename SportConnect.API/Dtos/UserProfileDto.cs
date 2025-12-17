using System.Collections.Generic;

namespace SportConnect.API.Dtos
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? Age { get; set; }
        public string? Description { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public List<UserSportDto> Sports { get; set; } = new();
    }
}
