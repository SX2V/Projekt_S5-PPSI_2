using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class UpdateLocationDto
    {
        [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
        public double Longitude { get; set; }

        [Range(0, 100, ErrorMessage = "Search radius must be between 0 and 100 kilometers.")]
        public int SearchRadiusKm { get; set; }
    }
}
