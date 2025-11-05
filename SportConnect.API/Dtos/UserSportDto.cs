namespace SportConnect.API.Dtos
{
    public class UserSportDto
    {
        public Guid SportId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? TypicalDistanceKm { get; set; }
    }
}
