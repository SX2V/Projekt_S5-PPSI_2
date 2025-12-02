using Microsoft.EntityFrameworkCore;
using SportConnect.API.Models;

namespace SportConnect.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Sport> Sports { get; set; }
        public DbSet<UserSport> UserSports { get; set; }
        public DbSet<MatchRequest> MatchRequests { get; set; }
        public DbSet<TrainingRequest> TrainingRequests { get; set; }
        public DbSet<ActionLog> ActionLogs { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserSport>()
                .HasKey(us => new { us.UserId, us.SportId });

            modelBuilder.Entity<UserSport>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserSports)
                .HasForeignKey(us => us.UserId);

            modelBuilder.Entity<UserSport>()
                .HasOne(us => us.Sport)
                .WithMany(s => s.UserSports)
                .HasForeignKey(us => us.SportId);
        }

    }
}
