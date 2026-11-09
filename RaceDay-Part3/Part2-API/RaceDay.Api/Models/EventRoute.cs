namespace RaceDay.Api.Models;

/// <summary>
/// Maps to the Routes table. Named EventRoute in C# because ASP.NET Core already has a type called Route.
/// </summary>
public class EventRoute
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }

    public Event Event { get; set; } = null!;
}
