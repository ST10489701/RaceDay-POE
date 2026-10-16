using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Event endpoints. Anyone can view events; only Organisers can create, update and delete their own.</summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EventsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists events, soonest first.</summary>
    /// <remarks>Optional query parameters: eventType ("Run", "Walk" or "Cycle") and upcomingOnly (true hides past events).</remarks>
    /// <response code="200">Returns the list of events.</response>
    /// <response code="400">Unknown eventType.</response>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<EventResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<EventResponse>>> GetEvents([FromQuery] string? eventType, [FromQuery] bool upcomingOnly = false)
    {
        var query = _db.Events.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = EventTypes.All.FirstOrDefault(t => string.Equals(t, eventType, StringComparison.OrdinalIgnoreCase));
            if (type is null)
            {
                return Problem(detail: "eventType must be 'Run', 'Walk' or 'Cycle'.", statusCode: StatusCodes.Status400BadRequest);
            }

            query = query.Where(e => e.EventType == type);
        }

        if (upcomingOnly)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            query = query.Where(e => e.EventDate >= today);
        }

        return await query.OrderBy(e => e.EventDate).Select(EventResponse.Projection).ToListAsync();
    }

    /// <summary>Gets one event with its categories and routes.</summary>
    /// <response code="200">Returns the event details.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDetailResponse>> GetEvent(int id)
    {
        var ev = await _db.Events.AsNoTracking()
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .Include(e => e.Routes)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null)
        {
            return NotFound();
        }

        return EventDetailResponse.FromDetail(ev);
    }

    /// <summary>Creates a new event owned by the logged-in Organiser.</summary>
    /// <remarks>Send eventName, description, eventDate (yyyy-MM-dd, not in the past), location, distanceKm and eventType ("Run", "Walk" or "Cycle").</remarks>
    /// <response code="201">Event created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as a Participant.</response>
    [HttpPost]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventResponse>> CreateEvent([FromBody] EventRequest request)
    {
        var (error, type) = ValidateRequest(request, isCreate: true);
        if (error is not null)
        {
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        var ev = new Event
        {
            OrganiserId = User.GetUserId(),
            EventName = request.EventName.Trim(),
            Description = request.Description.Trim(),
            EventDate = request.EventDate!.Value,
            Location = request.Location.Trim(),
            DistanceKm = request.DistanceKm!.Value,
            EventType = type,
            CreatedAt = DateTime.UtcNow
        };

        _db.Events.Add(ev);
        await _db.SaveChangesAsync();
        await _db.Entry(ev).Reference(e => e.Organiser).LoadAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, EventResponse.From(ev));
    }

    /// <summary>Updates an event. Only the Organiser who created it can do this.</summary>
    /// <response code="200">Returns the updated event.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(EventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventResponse>> UpdateEvent(int id, [FromBody] EventRequest request)
    {
        var ev = await _db.Events.Include(e => e.Organiser).FirstOrDefaultAsync(e => e.EventId == id);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        var (error, type) = ValidateRequest(request, isCreate: false);
        if (error is not null)
        {
            return Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        ev.EventName = request.EventName.Trim();
        ev.Description = request.Description.Trim();
        ev.EventDate = request.EventDate!.Value;
        ev.Location = request.Location.Trim();
        ev.DistanceKm = request.DistanceKm!.Value;
        ev.EventType = type;

        await _db.SaveChangesAsync();
        return EventResponse.From(ev);
    }

    /// <summary>Deletes an event together with its categories, routes, enrolments and results. Only the owning Organiser can do this.</summary>
    /// <response code="200">Event deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> DeleteEvent(int id)
    {
        var ev = await _db.Events
            .Include(e => e.Categories)
            .Include(e => e.Routes)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        var categoryIds = ev.Categories.Select(c => c.CategoryId).ToList();
        var enrolments = await _db.EventEnrolments.Where(en => categoryIds.Contains(en.CategoryId)).ToListAsync();
        var enrolmentIds = enrolments.Select(en => en.EnrolmentId).ToList();
        var results = await _db.Results.Where(r => enrolmentIds.Contains(r.EnrolmentId)).ToListAsync();

        // Children first; categories and routes are removed together with the event.
        _db.Results.RemoveRange(results);
        _db.EventEnrolments.RemoveRange(enrolments);
        _db.Events.Remove(ev);
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse("Event deleted."));
    }

    private static (string? Error, string EventType) ValidateRequest(EventRequest request, bool isCreate)
    {
        var type = EventTypes.All.FirstOrDefault(t => string.Equals(t, request.EventType, StringComparison.OrdinalIgnoreCase));
        if (type is null)
        {
            return ("Event type must be 'Run', 'Walk' or 'Cycle'.", string.Empty);
        }

        if (isCreate && request.EventDate!.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            return ("Event date cannot be in the past.", type);
        }

        return (null, type);
    }
}
