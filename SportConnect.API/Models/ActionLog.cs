using System;

namespace SportConnect.API.Models
{
    public class ActionLog
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }   
        public string? Action { get; set; } 
        public DateTime Timestamp { get; set; } = DateTime.UtcNow; 
    }
}
