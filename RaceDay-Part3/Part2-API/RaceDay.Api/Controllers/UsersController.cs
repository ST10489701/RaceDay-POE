using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Extensions;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>User profile endpoints. Both roles can view and update their own profile.</summary>
[ApiController]
[Route("api/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _db;
    private readonly IBlobStorageService _blobs;

    public UsersController(RaceDayDbContext db, IBlobStorageService blobs)
    {
        _db = db;
        _blobs = blobs;
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

    /// <summary>Uploads (or replaces) the logged-in user's profile picture.</summary>
    /// <remarks>Send the image as multipart/form-data in a field called "file" (JPEG, PNG, WebP or GIF, up to 5 MB). The image is stored in Azure Blob Storage.</remarks>
    /// <response code="200">Returns the updated profile (profilePicture holds the stored image path).</response>
    /// <response code="400">No file, not a supported image, or larger than 5 MB.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Tried to change someone else's picture.</response>
    /// <response code="404">User not found.</response>
    /// <response code="503">File storage is currently unavailable.</response>
    [HttpPost("{id:int}/profile-picture")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<UserResponse>> UploadProfilePicture(int id, IFormFile file)
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

        var image = await ImageFileValidator.ValidateAsync(file);
        if (image.Error is not null)
        {
            return Problem(detail: image.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        var blobPath = $"profiles/user-{id}-{Guid.NewGuid():N}{image.Extension}";
        try
        {
            await using var stream = file.OpenReadStream();
            await _blobs.UploadAsync(blobPath, stream, image.ContentType);
        }
        catch (StorageUnavailableException)
        {
            return Problem(detail: "File storage is currently unavailable. Please try again later.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var oldPicture = user.ProfilePicture;
        user.ProfilePicture = blobPath;
        await _db.SaveChangesAsync();

        if (!string.IsNullOrEmpty(oldPicture))
        {
            try
            {
                await _blobs.DeleteAsync(oldPicture);
            }
            catch (StorageUnavailableException)
            {
                // The database is already updated; a leftover image in storage is harmless.
            }
        }

        return UserResponse.From(user);
    }
}
