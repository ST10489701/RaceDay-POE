using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>Details needed to create or update an event.</summary>
public class EventRequest
{
    [Required, StringLength(150)]
    public string EventName { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Format: yyyy-MM-dd. New events cannot be in the past.</summary>
    [Required]
    public DateOnly? EventDate { get; set; }

    [Required, StringLength(150)]
    public string Location { get; set; } = string.Empty;

    /// <summary>Total distance in kilometres (0.01 to 999.99).</summary>
    [Required, Range(0.01, 999.99)]
    public decimal? DistanceKm { get; set; }

    /// <summary>One of "Run", "Walk" or "Cycle".</summary>
    [Required]
    public string EventType { get; set; } = string.Empty;
}

/// <summary>An event as returned by the API.</summary>
public class EventResponse
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string OrganiserName { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateOnly EventDate { get; set; }
    public string? Location { get; set; }
    public decimal DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    /// <summary>Used inside database queries so only the needed columns are read.</summary>
    public static readonly Expression<Func<Event, EventResponse>> Projection = e => new EventResponse
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser.FullName,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        CreatedAt = e.CreatedAt
    };

    public static EventResponse From(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        CreatedAt = e.CreatedAt
    };
}

/// <summary>An event together with its categories and routes.</summary>
public class EventDetailResponse : EventResponse
{
    public List<CategoryResponse> Categories { get; set; } = new();
    public List<RouteResponse> Routes { get; set; } = new();

    public static EventDetailResponse FromDetail(Event e) => new()
    {
        EventId = e.EventId,
        OrganiserId = e.OrganiserId,
        OrganiserName = e.Organiser?.FullName ?? string.Empty,
        EventName = e.EventName,
        Description = e.Description,
        EventDate = e.EventDate,
        Location = e.Location,
        DistanceKm = e.DistanceKm,
        EventType = e.EventType,
        CreatedAt = e.CreatedAt,
        Categories = e.Categories.OrderBy(c => c.CategoryId).Select(CategoryResponse.From).ToList(),
        Routes = e.Routes.OrderBy(r => r.RouteId).Select(RouteResponse.From).ToList()
    };
}

/// <summary>Details needed to create or update a category.</summary>
public class CategoryRequest
{
    /// <summary>For example "Under 20", "Senior", "10km" or "21km".</summary>
    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Leave empty for age-based categories.</summary>
    [Range(0.01, 999.99)]
    public decimal? DistanceKm { get; set; }

    [Range(0, 999999.99)]
    public decimal EntryFee { get; set; }

    /// <summary>Optional limit on confirmed enrolments in this category.</summary>
    [Range(1, 1000000)]
    public int? MaxParticipants { get; set; }
}

/// <summary>A category as returned by the API.</summary>
public class CategoryResponse
{
    public int CategoryId { get; set; }
    public int EventId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int? MaxParticipants { get; set; }

    public static CategoryResponse From(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        EntryFee = c.EntryFee,
        MaxParticipants = c.MaxParticipants
    };
}

/// <summary>Route/course information for an event.</summary>
public class RouteResponse
{
    public int RouteId { get; set; }
    public int EventId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int? ElevationGainM { get; set; }
    public string? MapUrl { get; set; }

    public static RouteResponse From(EventRoute r) => new()
    {
        RouteId = r.RouteId,
        EventId = r.EventId,
        RouteName = r.RouteName,
        DistanceKm = r.DistanceKm,
        ElevationGainM = r.ElevationGainM,
        MapUrl = r.MapUrl
    };
}
