using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class UpdateLocationDto
    {
        [Range(-90, 90, ErrorMessage = "UpdateLocationLatitudeRange")]
        public double Latitude { get; set; }

        [Range(-180, 180, ErrorMessage = "UpdateLocationLongitudeRange")]
        public double Longitude { get; set; }

        [Range(0, 100, ErrorMessage = "UpdateLocationSearchRadiusRange")]
        public int SearchRadiusKm { get; set; }
    }
}
