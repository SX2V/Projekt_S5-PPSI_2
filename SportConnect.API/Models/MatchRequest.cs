using System;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Models
{
    public enum MatchRequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class MatchRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required(ErrorMessage = "MatchRequestFromUserRequired")]
        public Guid FromUserId { get; set; }

        [Required(ErrorMessage = "MatchRequestToUserRequired")]
        public Guid ToUserId { get; set; }

        [Required(ErrorMessage = "MatchRequestSportRequired")]
        public Guid SportId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public MatchRequestStatus Status { get; set; } = MatchRequestStatus.Pending;

        public Sport Sport { get; set; } = null!;
    }
}
