namespace RaceDay.Api.Models;

/// <summary>Maps to the Events table.</summary>
public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string? Location { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = EventTypes.Run;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User Organiser { get; set; } = null!;
    public List<Category> Categories { get; set; } = new();
    public List<EventRoute> Routes { get; set; } = new();
}
