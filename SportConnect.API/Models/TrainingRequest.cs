using System;

namespace SportConnect.API.Models
{
    public class TrainingRequest
    {
        public Guid Id { get; set; }

        public Guid SenderId { get; set; }
        public User? Sender { get; set; }

        public Guid ReceiverId { get; set; }
        public User? Receiver { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Pending";
    }
}
