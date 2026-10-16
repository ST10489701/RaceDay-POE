using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>User profile endpoints. Both roles can view and update their own profile.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;

    public UsersController(RaceDayDbContext db)
    {
        _db = db;
    }

    /// <summary>Gets a user's profile.</summary>
    /// <remarks>Any logged-in user can view their own profile. Organisers can also view other users' profiles.</remarks>
    /// <response code="200">Returns the profile.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">A Participant tried to view someone else's profile.</response>
    /// <response code="404">User not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> GetProfile(int id)
    {
        if (User.GetUserId() != id && !User.IsInRole(Roles.Organiser))
        {
            return Forbid();
        }

        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null)
        {
            return NotFound();
        }

        return UserResponse.From(user);
    }

    /// <summary>Updates the logged-in user's own profile.</summary>
    /// <remarks>Send fullName and optional phoneNumber. Email and role cannot be changed.</remarks>
    /// <response code="200">Returns the updated profile.</response>
    /// <response code="400">Validation failed.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Tried to update someone else's profile.</response>
    /// <response code="404">User not found.</response>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> UpdateProfile(int id, [FromBody] UpdateProfileRequest request)
    {
        if (User.GetUserId() != id)
        {
            return Forbid();
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user is null)
        {
            return NotFound();
        }

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        await _db.SaveChangesAsync();

        return UserResponse.From(user);
    }
}
