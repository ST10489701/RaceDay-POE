using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Serves images stored in Azure Blob Storage. The MVC site fetches them from here, never from Azure directly.</summary>
[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private static readonly Regex SafeFileName = new("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.Compiled);

    private readonly IBlobStorageService _blobs;

    public ImagesController(IBlobStorageService blobs)
    {
        _blobs = blobs;
    }

    /// <summary>Downloads a stored image.</summary>
    /// <remarks>Use the path returned in an event's bannerImage or a user's profilePicture, for example /api/images/banners/event-1-abc.png. Only the "banners" and "profiles" folders can be read.</remarks>
    /// <response code="200">Returns the image.</response>
    /// <response code="404">No such image.</response>
    /// <response code="503">File storage is currently unavailable.</response>
    [HttpGet("{folder}/{fileName}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetImage(string folder, string fileName)
    {
        if ((folder != "banners" && folder != "profiles") || !SafeFileName.IsMatch(fileName) || fileName.Contains(".."))
        {
            return NotFound();
        }

        try
        {
            var blob = await _blobs.DownloadAsync($"{folder}/{fileName}");
            if (blob is null)
            {
                return NotFound();
            }

            Response.Headers.CacheControl = "public, max-age=3600";
            return File(blob.Content, blob.ContentType);
        }
        catch (StorageUnavailableException)
        {
            return Problem(detail: "File storage is currently unavailable. Please try again later.", statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
