using System;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class AddSportDto
    {
        [Required(ErrorMessage = "SportId is required.")]
        public Guid SportId { get; set; }

        [Range(0, 500, ErrorMessage = "Typical distance must be between 0 and 500 km.")]
        public int? TypicalDistanceKm { get; set; }
    }
}