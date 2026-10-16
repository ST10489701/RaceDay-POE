namespace RaceDay.Api.Models;

/// <summary>Maps to the Categories table. A distance or age category inside one event.</summary>
public class Category
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Null for age-based categories such as "Under 20".</summary>
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }

    public Event Event { get; set; } = null!;
    public List<EventEnrolment> Enrolments { get; set; } = new();
}
