namespace SportConnect.API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Email { get; set; }
        public required string Name { get; set; }
        public bool IsAvailableNow { get; set; }
        public int SearchRadiusKm { get; set; }
    }
}
