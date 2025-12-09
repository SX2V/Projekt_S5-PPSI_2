using SportConnect.API.Models;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Dtos
{
    public class MatchRequestStatusDto
    {
        [Required(ErrorMessage = "MatchRequestStatusRequired")]
        public MatchRequestStatus Status { get; set; }
    }

}
