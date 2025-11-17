using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Models
{
    public class UserSport
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Range(0, 500, ErrorMessage = "Typical distance must be between 0 and 500 km.")]
        public int TypicalDistanceKm { get; set; } = 0;
        public Guid SportId { get; set; }
        public Sport Sport { get; set; } = null!;
    }

}
