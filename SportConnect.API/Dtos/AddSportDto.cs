using System;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class AddSportDto
    {
        [Required(ErrorMessage = "AddSportIdRequired")]
        public Guid SportId { get; set; }

        [Range(0, 500, ErrorMessage = "AddSportTypicalDistanceRange")]
        public int? TypicalDistanceKm { get; set; }
    }
}