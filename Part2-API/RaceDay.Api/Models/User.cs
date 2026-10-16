namespace RaceDay.Api.Models;

/// <summary>Maps to the Users table. Holds both Organisers and Participants (see Role).</summary>
public class User
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = Roles.Participant;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Event> EventsOrganised { get; set; } = new();
    public List<EventEnrolment> Enrolments { get; set; } = new();
}
