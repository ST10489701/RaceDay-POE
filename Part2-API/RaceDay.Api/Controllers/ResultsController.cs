using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Race results. Organisers capture them after an event; Participants view their own.</summary>
[ApiController]
[Authorize]
[Produces("application/json")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public ResultsController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Captures the result for one enrolment. Only the Organiser who owns the event can do this.</summary>
    /// <remarks>Send enrolmentId, status ("Finished", "DNF" or "DQ"), and for "Finished" also finishTime (hh:mm:ss) and position. Results can only be captured on or after the event date.</remarks>
    /// <response code="201">Result captured.</response>
    /// <response code="400">Validation failed, or the event has not taken place yet.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Enrolment not found.</response>
    /// <response code="409">A result already exists for this enrolment, or the position is already taken in this category.</response>
    [HttpPost("~/api/results")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ResultResponse>> CaptureResult([FromBody] CaptureResultRequest request)
    {
        var enrolment = await _db.EventEnrolments
            .Include(e => e.Category).ThenInclude(c => c.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

        if (enrolment is null)
        {
            return Problem(detail: "Enrolment not found.", statusCode: StatusCodes.Status404NotFound);
        }

        var ev = enrolment.Category.Event;
        if (ev.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        var status = ResultStatuses.All.FirstOrDefault(s => string.Equals(s, request.Status, StringComparison.OrdinalIgnoreCase));
        if (status is null)
        {
            return Problem(detail: "Status must be 'Finished', 'DNF' or 'DQ'.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (enrolment.Status != EnrolmentStatuses.Confirmed)
        {
            return Problem(detail: "A cancelled enrolment cannot receive a result.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (ev.EventDate > DateOnly.FromDateTime(DateTime.Today))
        {
            return Problem(detail: "Results can only be captured on or after the event date.", statusCode: StatusCodes.Status400BadRequest);
        }

        if (enrolment.Result is not null)
        {
            return Problem(detail: "A result has already been captured for this enrolment.", statusCode: StatusCodes.Status409Conflict);
        }

        TimeSpan? finishTime = null;
        int? position = null;

        if (status == ResultStatuses.Finished)
        {
            if (request.FinishTime is null || request.FinishTime <= TimeSpan.Zero || request.FinishTime >= TimeSpan.FromHours(24))
            {
                return Problem(detail: "A finished result needs a finishTime between 00:00:01 and 23:59:59.", statusCode: StatusCodes.Status400BadRequest);
            }

            if (request.Position is null)
            {
                return Problem(detail: "A finished result needs a position.", statusCode: StatusCodes.Status400BadRequest);
            }

            finishTime = request.FinishTime;
            position = request.Position;

            var positionTaken = await _db.Results.AnyAsync(r => r.Position == position && r.Enrolment.CategoryId == enrolment.CategoryId);
            if (positionTaken)
            {
                return Problem(detail: "That position is already taken in this category.", statusCode: StatusCodes.Status409Conflict);
            }
        }

        var result = new RaceResult
        {
            EnrolmentId = enrolment.EnrolmentId,
            FinishTime = finishTime,
            Position = position,
            Status = status
        };

        _db.Results.Add(result);
        await _db.SaveChangesAsync();

        var response = await _db.Results.AsNoTracking()
            .Where(r => r.ResultId == result.ResultId)
            .Select(ResultResponse.Projection)
            .FirstAsync();

        return Created($"/api/results/{enrolment.EnrolmentId}", response);
    }

    /// <summary>Gets the result for one enrolment.</summary>
    /// <remarks>The Participant who owns the enrolment, or the Organiser who owns the event, can view it.</remarks>
    /// <response code="200">Returns the result.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">The result belongs to someone else.</response>
    /// <response code="404">Enrolment not found, or no result captured yet.</response>
    [HttpGet("~/api/results/{enrolmentId:int}")]
    [ProducesResponseType(typeof(ResultResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResultResponse>> GetResult(int enrolmentId)
    {
        var info = await _db.EventEnrolments.AsNoTracking()
            .Where(e => e.EnrolmentId == enrolmentId)
            .Select(e => new { e.ParticipantId, OrganiserId = e.Category.Event.OrganiserId })
            .FirstOrDefaultAsync();

        if (info is null)
        {
            return NotFound();
        }

        var userId = User.GetUserId();
        var isOwnerParticipant = info.ParticipantId == userId;
        var isEventOrganiser = User.IsInRole(Roles.Organiser) && info.OrganiserId == userId;
        if (!isOwnerParticipant && !isEventOrganiser)
        {
            return Forbid();
        }

        var result = await _db.Results.AsNoTracking()
            .Where(r => r.EnrolmentId == enrolmentId)
            .Select(ResultResponse.Projection)
            .FirstOrDefaultAsync();

        if (result is null)
        {
            return NotFound();
        }

        return result;
    }

    /// <summary>Gets a Participant's full result history across all events.</summary>
    /// <remarks>A user can view their own history. An Organiser can view a participant's results for events that Organiser runs.</remarks>
    /// <response code="200">Returns the results, newest event first.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">A Participant tried to view someone else's results.</response>
    [HttpGet("~/api/users/{userId:int}/results")]
    [ProducesResponseType(typeof(List<ResultResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<ResultResponse>>> GetUserResults(int userId)
    {
        var callerId = User.GetUserId();
        if (callerId != userId && !User.IsInRole(Roles.Organiser))
        {
            return Forbid();
        }

        var query = _db.Results.AsNoTracking().Where(r => r.Enrolment.ParticipantId == userId);
        if (callerId != userId)
        {
            query = query.Where(r => r.Enrolment.Category.Event.OrganiserId == callerId);
        }

        return await query
            .OrderByDescending(r => r.Enrolment.Category.Event.EventDate)
            .Select(ResultResponse.Projection)
            .ToListAsync();
    }
}
