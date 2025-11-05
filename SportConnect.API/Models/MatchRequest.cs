namespace SportConnect.API.Models
{
    public class MatchRequest
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public Guid SportId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";
        public Sport Sport { get; set; } = null!;

    }

}
