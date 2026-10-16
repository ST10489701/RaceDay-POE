using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

/// <summary>
/// Entity Framework Core database context. The fluent configuration below makes the generated
/// database match the Part 1 ERD and SQL script (table names, column types, keys, constraints).
/// </summary>
public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventRoute> Routes => Set<EventRoute>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<EventEnrolment> EventEnrolments => Set<EventEnrolment>();
    public DbSet<RaceResult> Results => Set<RaceResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", "[Role] IN ('Organiser', 'Participant')"));
            entity.HasKey(u => u.UserId);
            entity.Property(u => u.FullName).HasMaxLength(100).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(150).IsRequired();
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.PasswordHash).HasMaxLength(255).IsRequired();
            entity.Property(u => u.PhoneNumber).HasMaxLength(20);
            entity.Property(u => u.Role).HasMaxLength(20).IsRequired();
            entity.Property(u => u.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", t => t.HasCheckConstraint("CK_Events_EventType", "[EventType] IN ('Run', 'Walk', 'Cycle')"));
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventName).HasMaxLength(150).IsRequired();
            entity.Property(e => e.EventDate).HasColumnType("date");
            entity.Property(e => e.Location).HasMaxLength(150);
            entity.Property(e => e.DistanceKm).HasColumnType("decimal(5,2)");
            entity.Property(e => e.EventType).HasMaxLength(10).IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");

            // One Organiser (User) has many Events. No cascade: matches the Part 1 FK_Events_Users.
            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.EventsOrganised)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EventRoute>(entity =>
        {
            entity.ToTable("Routes");
            entity.HasKey(r => r.RouteId);
            entity.Property(r => r.RouteName).HasMaxLength(100).IsRequired();
            entity.Property(r => r.DistanceKm).HasColumnType("decimal(5,2)");
            entity.Property(r => r.MapUrl).HasMaxLength(255);

            entity.HasOne(r => r.Event)
                  .WithMany(e => e.Routes)
                  .HasForeignKey(r => r.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(c => c.CategoryId);
            entity.Property(c => c.CategoryName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.DistanceKm).HasColumnType("decimal(5,2)");
            entity.Property(c => c.EntryFee).HasColumnType("decimal(8,2)").HasDefaultValue(0m);

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<EventEnrolment>(entity =>
        {
            entity.ToTable("EventEnrolments", t => t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Confirmed', 'Cancelled')"));
            entity.HasKey(e => e.EnrolmentId);
            entity.Property(e => e.BibNumber).HasMaxLength(10);
            entity.Property(e => e.EnrolmentDate).HasColumnType("datetime").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status).HasMaxLength(20).IsRequired().HasDefaultValue(EnrolmentStatuses.Confirmed);

            entity.HasOne(e => e.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(e => e.ParticipantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            // A participant can only enter the same category once (UQ_Enrolments_Participant_Category in Part 1).
            entity.HasIndex(e => new { e.ParticipantId, e.CategoryId })
                  .IsUnique()
                  .HasDatabaseName("UQ_Enrolments_Participant_Category");
        });

        modelBuilder.Entity<RaceResult>(entity =>
        {
            entity.ToTable("Results", t => t.HasCheckConstraint("CK_Results_Status", "[Status] IN ('Finished', 'DNF', 'DQ')"));
            entity.HasKey(r => r.ResultId);
            entity.Property(r => r.FinishTime).HasColumnType("time");
            entity.Property(r => r.Status).HasMaxLength(20).IsRequired().HasDefaultValue(ResultStatuses.Finished);

            // One-to-one: each enrolment has at most one result.
            entity.HasOne(r => r.Enrolment)
                  .WithOne(e => e.Result)
                  .HasForeignKey<RaceResult>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(r => r.EnrolmentId).IsUnique();
        });
    }
}
