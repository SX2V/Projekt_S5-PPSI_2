namespace SportConnect.API.Models
{
    public class Sport
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? TypicalDistanceKm { get; set; }

        public ICollection<UserSport> UserSports { get; set; } = new List<UserSport>();
    }

}
