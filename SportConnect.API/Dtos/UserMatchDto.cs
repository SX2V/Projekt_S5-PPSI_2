namespace SportConnect.API.Dtos
{
    public class UserMatchDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsAvailableNow { get; set; }
        public int SearchRadiusKm { get; set; }

        public double DistanceKm { get; set; }
        public List<Guid> SharedSportIds { get; set; } = new();
    }
}
