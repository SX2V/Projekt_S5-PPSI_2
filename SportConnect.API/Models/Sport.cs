using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Models
{
    public class Sport
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "SportNameRequired")]
        [MaxLength(100, ErrorMessage = "SportNameMaxLength")]
        public string Name { get; set; } = string.Empty;

        [MaxLength(250, ErrorMessage = "SportDescriptionMaxLength")]
        public string? Description { get; set; }

        [Range(0, 1000, ErrorMessage = "SportTypicalDistanceRange")]
        public int? TypicalDistanceKm { get; set; }

        public ICollection<UserSport> UserSports { get; set; } = new List<UserSport>();
    }
}
