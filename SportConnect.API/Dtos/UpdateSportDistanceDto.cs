using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class UpdateSportDistanceDto
    {
        [Range(0, 500, ErrorMessage = "UpdateSportDistanceRange")]
        public int TypicalDistanceKm { get; set; }
    }
}
