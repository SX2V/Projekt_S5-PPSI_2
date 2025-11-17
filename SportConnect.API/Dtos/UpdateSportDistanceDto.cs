using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class UpdateSportDistanceDto
    {
        [Range(0, 500, ErrorMessage = "Typical distance must be between 0 and 500 km.")]
        public int TypicalDistanceKm { get; set; }
    }
}
