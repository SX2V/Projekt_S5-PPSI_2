namespace SportConnect.API.Dtos
{
    public class MatchRequestViewDto
    {
        public Guid Id { get; set; }
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public Guid SportId { get; set; }
        public string SportName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

}
