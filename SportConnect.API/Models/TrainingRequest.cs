using System;
using System.ComponentModel.DataAnnotations;

namespace SportConnect.API.Models
{
    public enum TrainingRequestStatus
    {
        Pending,
        Accepted,
        Rejected
    }

    public class TrainingRequest
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "TrainingRequestSenderRequired")]
        public Guid SenderId { get; set; }
        public User? Sender { get; set; }

        [Required(ErrorMessage = "TrainingRequestReceiverRequired")]
        public Guid ReceiverId { get; set; }
        public User? Receiver { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public TrainingRequestStatus Status { get; set; } = TrainingRequestStatus.Pending;

        public DateTime? RespondedAt { get; set; }

        public DateTime? TrainingDateTime { get; set; }
        public string? Location { get; set; }
        public string? Message { get; set; }
    }
}
