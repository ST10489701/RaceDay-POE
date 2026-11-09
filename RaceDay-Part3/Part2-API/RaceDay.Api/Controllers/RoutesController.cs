using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

/// <summary>Route/course information for events (used for race-day preparation).</summary>
[ApiController]
[Produces("application/json")]
public class RoutesController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public RoutesController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Lists the routes (distance, elevation, map link) of an event.</summary>
    /// <response code="200">Returns the routes.</response>
    /// <response code="404">Event not found.</response>
    [HttpGet("~/api/events/{eventId:int}/routes")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<RouteResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<RouteResponse>>> GetRoutes(int eventId)
    {
        if (!await _db.Events.AnyAsync(e => e.EventId == eventId))
        {
            return NotFound();
        }

        var routes = await _db.Routes.AsNoTracking().Where(r => r.EventId == eventId).OrderBy(r => r.RouteId).ToListAsync();
        return routes.Select(RouteResponse.From).ToList();
    }
}
