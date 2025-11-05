namespace SportConnect.API.Models
{
    public class UserSport
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public Guid SportId { get; set; }
        public Sport Sport { get; set; } = null!;
    }

}
