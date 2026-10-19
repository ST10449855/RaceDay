using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Enrolment> Enrolments { get; set; }
        public DbSet<Result> Results { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add CHECK constraint for category age range per feedback
            modelBuilder.Entity<Category>()
                .ToTable(b => b.HasCheckConstraint("CK_Category_AgeRange", "[MinAge] <= [MaxAge]"));

            modelBuilder.Entity<Enrolment>()
                .HasOne(e => e.Participant)
                .WithMany()
                .HasForeignKey(e => e.ParticipantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Enforce 0..1 relationship for Result per feedback[cite: 7]
            modelBuilder.Entity<Result>()
                .HasOne(r => r.Enrolment)
                .WithOne(e => e.Result)
                .HasForeignKey<Result>(r => r.EnrolmentId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public enum UserRole { Organiser, Participant }
    public enum EventType { Run, Walk, Cycle }

    public class User
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        [Required, EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? ProfilePictureUrl { get; set; } // Added per feedback[cite: 7]
    }

    public class Event
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
        public EventType Type { get; set; }
        public string? BannerUrl { get; set; } // Added per feedback[cite: 7]
        public int OrganiserId { get; set; }
        public List<Category> Categories { get; set; } = new();
    }

    public class Category
    {
        public int Id { get; set; }
        [Required] public string Name { get; set; } = string.Empty;
        public int MinAge { get; set; } // Added per feedback[cite: 7]
        public int MaxAge { get; set; } // Added per feedback[cite: 7]
        public int EventId { get; set; }
    }

    public class Enrolment
    {
        public int Id { get; set; }
        public int ParticipantId { get; set; }
        public User? Participant { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public Result? Result { get; set; } // 0..1 relationship[cite: 7]
    }

    public class Result
    {
        public int Id { get; set; }
        public int EnrolmentId { get; set; }
        public TimeSpan FinishTime { get; set; }
        public int Position { get; set; }
    }
}