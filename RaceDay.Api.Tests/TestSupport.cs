using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Tests;

public record SeededEvent(int EventId, int CategoryId, int OrganiserId, int EnrolmentId);

/// <summary>
/// Starts the real API in memory for each test class, but swaps SQL Server for an in-memory database
/// so the tests need no database server (this is also what lets them run in GitHub Actions).
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    public const string Password = "Password123!";
    public const string Organiser1Email = "organiser1@raceday.test";
    public const string Organiser2Email = "organiser2@raceday.test";
    public const string Participant1Email = "participant1@raceday.test";
    public const string Participant2Email = "participant2@raceday.test";

    private readonly string _databaseName = "RaceDayTests_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove the SQL Server registration made in Program.cs (works for EF Core 8, 9 and 10).
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GenericTypeArguments.Contains(typeof(RaceDayDbContext)) &&
                 d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))).ToList();

            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        var passwords = scope.ServiceProvider.GetRequiredService<IPasswordService>();

        db.Database.EnsureCreated();

        var hash = passwords.Hash(Password);
        db.Users.AddRange(
            new User { FullName = "Test Organiser One", Email = Organiser1Email, PasswordHash = hash, Role = Roles.Organiser },
            new User { FullName = "Test Organiser Two", Email = Organiser2Email, PasswordHash = hash, Role = Roles.Organiser },
            new User { FullName = "Test Participant One", Email = Participant1Email, PasswordHash = hash, Role = Roles.Participant },
            new User { FullName = "Test Participant Two", Email = Participant2Email, PasswordHash = hash, Role = Roles.Participant });
        db.SaveChanges();

        return host;
    }

    /// <summary>A client that keeps cookies, so the login session is remembered between requests.</summary>
    public HttpClient CreateApiClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public async Task<T> QueryDbAsync<T>(Func<RaceDayDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        return await query(db);
    }

    public Task<int> GetUserIdAsync(string email) =>
        QueryDbAsync(db => db.Users.Where(u => u.Email == email).Select(u => u.UserId).SingleAsync());

    /// <summary>Creates an event in the future with one category (optionally limited in size).</summary>
    public Task<SeededEvent> SeedUpcomingEventAsync(string organiserEmail = Organiser1Email, int? maxParticipants = null) =>
        SeedEventAsync(organiserEmail, DateOnly.FromDateTime(DateTime.Today.AddDays(30)), maxParticipants, null);

    /// <summary>Creates an event in the past with one category and one confirmed enrolment (no result yet).</summary>
    public Task<SeededEvent> SeedPastEventWithEnrolmentAsync(string organiserEmail = Organiser1Email, string participantEmail = Participant1Email) =>
        SeedEventAsync(organiserEmail, DateOnly.FromDateTime(DateTime.Today.AddDays(-10)), null, participantEmail);

    private async Task<SeededEvent> SeedEventAsync(string organiserEmail, DateOnly date, int? maxParticipants, string? participantEmail)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

        var organiser = await db.Users.SingleAsync(u => u.Email == organiserEmail);
        var ev = new Event
        {
            Organiser = organiser,
            EventName = $"Seeded Event {Guid.NewGuid():N}",
            Description = "Created for a test",
            EventDate = date,
            Location = "Test City",
            DistanceKm = 10m,
            EventType = EventTypes.Run
        };
        var category = new Category { Event = ev, CategoryName = "10km", DistanceKm = 10m, EntryFee = 100m, MaxParticipants = maxParticipants };
        db.Events.Add(ev);
        db.Categories.Add(category);

        EventEnrolment? enrolment = null;
        if (participantEmail is not null)
        {
            var participant = await db.Users.SingleAsync(u => u.Email == participantEmail);
            enrolment = new EventEnrolment { Participant = participant, Category = category, BibNumber = "1", Status = EnrolmentStatuses.Confirmed };
            db.EventEnrolments.Add(enrolment);
        }

        await db.SaveChangesAsync();
        return new SeededEvent(ev.EventId, category.CategoryId, organiser.UserId, enrolment?.EnrolmentId ?? 0);
    }
}

public static class TestExtensions
{
    /// <summary>Logs in through the real login endpoint so the client holds the session cookie.</summary>
    public static async Task<HttpClient> LoginAsync(this HttpClient client, string email, string password = RaceDayFactory.Password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        return client;
    }
}
