using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Data;

/// <summary>
/// Adds demo data the first time the API runs in Development (only if the Users table is empty),
/// so Swagger can be demonstrated straight away. Every demo account uses the password below.
/// </summary>
public static class DbSeeder
{
    public const string DemoPassword = "Password123!";

    public static async Task SeedAsync(RaceDayDbContext db, IPasswordService passwords)
    {
        if (await db.Users.AnyAsync())
        {
            return;
        }

        var hash = passwords.Hash(DemoPassword);
        var today = DateOnly.FromDateTime(DateTime.Today);

        // Users: 2 Organisers, 2 Participants
        var thandiwe = new User { FullName = "Thandiwe Nkosi", Email = "thandiwe.nkosi@raceday.co.za", PasswordHash = hash, PhoneNumber = "0821234567", Role = Roles.Organiser };
        var johan = new User { FullName = "Johan van der Merwe", Email = "johan.vdm@raceday.co.za", PasswordHash = hash, PhoneNumber = "0837654321", Role = Roles.Organiser };
        var lerato = new User { FullName = "Lerato Dlamini", Email = "lerato.dlamini@gmail.com", PasswordHash = hash, PhoneNumber = "0721112222", Role = Roles.Participant };
        var sipho = new User { FullName = "Sipho Khumalo", Email = "sipho.khumalo@gmail.com", PasswordHash = hash, PhoneNumber = "0733334444", Role = Roles.Participant };

        // Events: one in the past (so results can be captured) and two in the future
        var halfMarathon = new Event
        {
            Organiser = thandiwe,
            EventName = "Johannesburg City Half Marathon",
            Description = "A scenic half marathon through the streets of Johannesburg.",
            EventDate = today.AddDays(-30),
            Location = "Johannesburg, Gauteng",
            DistanceKm = 21.10m,
            EventType = EventTypes.Run
        };
        var funWalk = new Event
        {
            Organiser = thandiwe,
            EventName = "Soweto Community Fun Walk",
            Description = "A family-friendly community walk supporting local charities.",
            EventDate = today.AddDays(30),
            Location = "Soweto, Gauteng",
            DistanceKm = 5.00m,
            EventType = EventTypes.Walk
        };
        var cycleChallenge = new Event
        {
            Organiser = johan,
            EventName = "Cape Winelands Cycle Challenge",
            Description = "A charity cycling event through the Cape Winelands.",
            EventDate = today.AddDays(120),
            Location = "Stellenbosch, Western Cape",
            DistanceKm = 94.70m,
            EventType = EventTypes.Cycle
        };

        // Categories (2 per event; "Under 20" is an age category so it has no distance)
        var half21 = new Category { Event = halfMarathon, CategoryName = "21km Half Marathon", DistanceKm = 21.10m, EntryFee = 250m, MaxParticipants = 2000 };
        var run10 = new Category { Event = halfMarathon, CategoryName = "10km Fun Run", DistanceKm = 10m, EntryFee = 150m, MaxParticipants = 1500 };
        var walk5 = new Category { Event = funWalk, CategoryName = "5km Fun Walk", DistanceKm = 5m, EntryFee = 50m, MaxParticipants = 1000 };
        var under20 = new Category { Event = funWalk, CategoryName = "Under 20", DistanceKm = null, EntryFee = 0m, MaxParticipants = 500 };
        var ride94 = new Category { Event = cycleChallenge, CategoryName = "94km Challenge Ride", DistanceKm = 94.70m, EntryFee = 450m, MaxParticipants = 800 };
        var ride45 = new Category { Event = cycleChallenge, CategoryName = "45km Half Challenge Ride", DistanceKm = 45m, EntryFee = 300m, MaxParticipants = 600 };

        // One route per event
        var route1 = new EventRoute { Event = halfMarathon, RouteName = "JHB Half Marathon Route", DistanceKm = 21.10m, ElevationGainM = 180, MapUrl = "https://maps.raceday.co.za/jhb-half" };
        var route2 = new EventRoute { Event = funWalk, RouteName = "Soweto Fun Walk Route", DistanceKm = 5.00m, ElevationGainM = 40, MapUrl = "https://maps.raceday.co.za/soweto-walk" };
        var route3 = new EventRoute { Event = cycleChallenge, RouteName = "Winelands Cycle Route", DistanceKm = 94.70m, ElevationGainM = 620, MapUrl = "https://maps.raceday.co.za/winelands-cycle" };

        // Enrolments
        var leratoHalf = new EventEnrolment { Participant = lerato, Category = half21, BibNumber = "1", Status = EnrolmentStatuses.Confirmed, EnrolmentDate = DateTime.UtcNow.AddDays(-60) };
        var siphoRun = new EventEnrolment { Participant = sipho, Category = run10, BibNumber = "2", Status = EnrolmentStatuses.Confirmed, EnrolmentDate = DateTime.UtcNow.AddDays(-55) };
        var leratoRide = new EventEnrolment { Participant = lerato, Category = ride94, BibNumber = "1", Status = EnrolmentStatuses.Confirmed, EnrolmentDate = DateTime.UtcNow.AddDays(-2) };

        // One result already captured (Sipho's result is left empty so the Organiser can capture it in the demo)
        var leratoResult = new RaceResult { Enrolment = leratoHalf, FinishTime = new TimeSpan(1, 45, 32), Position = 112, Status = ResultStatuses.Finished };

        db.AddRange(thandiwe, johan, lerato, sipho,
                    halfMarathon, funWalk, cycleChallenge,
                    half21, run10, walk5, under20, ride94, ride45,
                    route1, route2, route3,
                    leratoHalf, siphoRun, leratoRide,
                    leratoResult);

        await db.SaveChangesAsync();
    }
}
