namespace SportConnect.API.Dtos
{
    public class MatchRequestDto
    {
        public Guid FromUserId { get; set; }
        public Guid ToUserId { get; set; }
        public Guid SportId { get; set; }
    }

}
