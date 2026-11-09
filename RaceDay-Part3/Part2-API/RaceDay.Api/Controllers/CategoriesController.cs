using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Category endpoints (age or distance categories inside an event). Anyone can view; only the owning Organiser can change them.</summary>
[ApiController]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public CategoriesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists the categories of an event.</summary>
    /// <response code="200">Returns the categories.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("~/api/events/{eventId:int}/categories")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound();
        }

        var categories = await _db.Categories.AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.CategoryId)
            .ToListAsync();

        return categories.Select(CategoryResponse.From).ToList();
    }

    /// <summary>Adds a category to an event. Only the owning Organiser can do this.</summary>
    /// <remarks>Send categoryName, optional distanceKm (leave empty for age categories such as "Under 20"), entryFee and optional maxParticipants.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Event not found.</response>
    [HttpPost("~/api/events/{eventId:int}/categories")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(int eventId, [FromBody] CategoryRequest request)
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

        var category = new Category
        {
            EventId = eventId,
            CategoryName = request.CategoryName.Trim(),
            DistanceKm = request.DistanceKm,
            EntryFee = request.EntryFee,
            MaxParticipants = request.MaxParticipants
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { eventId }, CategoryResponse.From(category));
    }

    /// <summary>Updates a category. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">Returns the updated category.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">maxParticipants is lower than the number of confirmed enrolments.</response>
    [HttpPut("~/api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(int id, [FromBody] CategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return NotFound();
        }

        if (category.Event.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        if (request.MaxParticipants is int max)
        {
            var confirmed = await _db.EventEnrolments.CountAsync(e => e.CategoryId == id && e.Status == EnrolmentStatuses.Confirmed);
            if (max < confirmed)
            {
                return Problem(detail: $"This category already has {confirmed} confirmed enrolments.", statusCode: StatusCodes.Status409Conflict);
            }
        }

        category.CategoryName = request.CategoryName.Trim();
        category.DistanceKm = request.DistanceKm;
        category.EntryFee = request.EntryFee;
        category.MaxParticipants = request.MaxParticipants;

        await _db.SaveChangesAsync();
        return CategoryResponse.From(category);
    }

    /// <summary>Deletes a category that has no enrolments. Only the Organiser who owns the event can do this.</summary>
    /// <response code="200">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser, or the event belongs to another Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category has enrolments and cannot be deleted.</response>
    [HttpDelete("~/api/categories/{id:int}")]
    [Authorize(Roles = Roles.Organiser)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MessageResponse>> DeleteCategory(int id)
    {
        var category = await _db.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category is null)
        {
            return NotFound();
        }

        if (category.Event.OrganiserId != User.GetUserId())
        {
            return Forbid();
        }

        if (await _db.EventEnrolments.AnyAsync(e => e.CategoryId == id))
        {
            return Problem(detail: "This category has enrolments and cannot be deleted.", statusCode: StatusCodes.Status409Conflict);
        }

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        return Ok(new MessageResponse("Category deleted."));
    }
}
