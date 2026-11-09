using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Event enrolments. Participants enter events by choosing a category; Organisers see everyone entered in their events.</summary>
[ApiController]
[Produces("application/json")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public EnrolmentsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Enters the logged-in Participant into an event by selecting a category.</summary>
    /// <remarks>Send categoryId. The enrolment links the participant, the event and the category and gets a bib number. A participant can only be confirmed in one category per event.</remarks>
    /// <response code="201">Enrolled. Returns the enrolment including the bib number.</response>
    /// <response code="400">The event has already taken place.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    /// <response code="404">Event not found, or the category does not belong to this event.</response>
    /// <response code="409">Already enrolled in this event, or the category is full.</response>
    [HttpPost("~/api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(EnrolmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<EnrolmentResponse>> Enrol(int eventId, [FromBody] EnrolRequest request)
    {
        var participantId = User.GetUserId();

        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
        {
            return Problem(detail: "Event not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var category = await _db.Categories.FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == eventId);
        if (category is null)
        {
            return Problem(detail: "That category does not belong to this event.", statusCode: StatusCodes.Status404NotFound);
        }

        if (ev.EventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return Problem(detail: "This event has already taken place.", statusCode: StatusCodes.Status400BadRequest);
        }

        var existing = await _db.EventEnrolments
            .Where(e => e.ParticipantId == participantId && e.Category.EventId == eventId)
            .ToListAsync();

        if (existing.Any(e => e.Status == EnrolmentStatuses.Confirmed))
        {
            return Problem(detail: "You are already enrolled in this event.", statusCode: StatusCodes.Status409Conflict);
        }

        if (category.MaxParticipants is int max)
        {
            var taken = await _db.EventEnrolments.CountAsync(e => e.CategoryId == category.CategoryId && e.Status == EnrolmentStatuses.Confirmed);
            if (taken >= max)
            {
                return Problem(detail: "This category is full.", statusCode: StatusCodes.Status409Conflict);
            }
        }

        EventEnrolment enrolment;
        var previous = existing.FirstOrDefault(e => e.CategoryId == category.CategoryId);
        if (previous is not null)
        {
            // Re-entering a category the participant had cancelled: reactivate the old enrolment.
            previous.Status = EnrolmentStatuses.Confirmed;
            previous.EnrolmentDate = DateTime.UtcNow;
            enrolment = previous;
        }
        else
        {
            var bibs = await _db.EventEnrolments
                .Where(e => e.Category.EventId == eventId)
                .Select(e => e.BibNumber)
                .ToListAsync();
            var nextBib = bibs.Select(b => int.TryParse(b, out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;

            enrolment = new EventEnrolment
            {
                ParticipantId = participantId,
                CategoryId = category.CategoryId,
                BibNumber = nextBib.ToString(),
                EnrolmentDate = DateTime.UtcNow,
                Status = EnrolmentStatuses.Confirmed
            };
            _db.EventEnrolments.Add(enrolment);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Problem(detail: "You are already enrolled in this category.", statusCode: StatusCodes.Status409Conflict);
        }

        var response = new EnrolmentResponse
        {
            EnrolmentId = enrolment.EnrolmentId,
            ParticipantId = participantId,
            ParticipantName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
            EventId = ev.EventId,
            EventName = ev.EventName,
            EventDate = ev.EventDate,
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            BibNumber = enrolment.BibNumber,
            EnrolmentDate = enrolment.EnrolmentDate,
            Status = enrolment.Status
        };

        return Created("/api/enrolments/my", response);
    }

    /// <summary>Lists the logged-in Participant's own enrolments, newest event first.</summary>
    /// <response code="200">Returns the enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser.</response>
    [HttpGet("~/api/enrolments/my")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetMyEnrolments()
    {
        var participantId = User.GetUserId();

        return await _db.EventEnrolments.AsNoTracking()
            .Where(e => e.ParticipantId == participantId)
            .OrderByDescending(e => e.Category.Event.EventDate)
            .Select(EnrolmentResponse.Projection)
            .ToListAsync();
    }

    /// <summary>Lists every enrolment for an event. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">Returns the enrolments.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("~/api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(List<EnrolmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<EnrolmentResponse>>> GetEventEnrolments(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev is null)
        {
            return NotFound();
        }

        if (ev.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        return await _db.EventEnrolments.AsNoTracking()
            .Where(e => e.Category.EventId == eventId)
            .OrderBy(e => e.EnrolmentId)
            .Select(EnrolmentResponse.Projection)
            .ToListAsync();
    }

    /// <summary>Cancels the logged-in Participant's own enrolment (status becomes "Cancelled").</summary>
    /// <response code="200">Enrolment cancelled.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Logged in as an Organiser, or the enrolment belongs to someone else.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">Already cancelled, or the event has already taken place.</response>
    [HttpDelete("~/api/enrolments/{id:int}")]
    [Authorize(Roles = Roles.Participant)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>> CancelEnrolment(int id)
    {
        var enrolment = await _db.EventEnrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == id);

        if (enrolment is null)
        {
            return NotFound();
        }

        if (enrolment.ParticipantId != User.GetUserId())
        {
            return Forbid();
        }

        if (enrolment.Status == EnrolmentStatuses.Cancelled)
        {
            return Problem(detail: "This enrolment is already cancelled.", statusCode: StatusCodes.Status409Conflict);
        }

        if (enrolment.Result is not null || enrolment.Category.Event.EventDate < DateOnly.FromDateTime(DateTime.Today))
        {
            return Problem(detail: "An enrolment cannot be cancelled after the event has taken place.", statusCode: StatusCodes.Status409Conflict);
        }

        enrolment.Status = EnrolmentStatuses.Cancelled;
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse("Enrolment cancelled."));
    }
}
