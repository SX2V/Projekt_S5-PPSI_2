using System.ComponentModel.DataAnnotations;
using System;

namespace SportConnect.API.Models
{
    public class UserSport
    {
        [Required(ErrorMessage = "UserSportUserRequired")]
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [Range(0, 500, ErrorMessage = "UserSportTypicalDistanceRange")]
        public int TypicalDistanceKm { get; set; } = 0;

        [Required(ErrorMessage = "UserSportSportRequired")]
        public Guid SportId { get; set; }
        public Sport Sport { get; set; } = null!;
    }
}
