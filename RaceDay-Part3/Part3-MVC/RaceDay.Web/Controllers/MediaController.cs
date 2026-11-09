using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Shows event banners and profile pictures. The images live in Azure Blob Storage, but this site never talks
/// to Azure: it asks the API for the image and passes it on to the browser.
/// </summary>
[AllowAnonymous]
public class MediaController : Controller
{
    private readonly IRaceDayApiClient _api;

    public MediaController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [HttpGet("/media/{folder}/{fileName}")]
    public async Task<IActionResult> Get(string folder, string fileName)
    {
        var image = await _api.GetImageAsync(folder, fileName);
        if (image is null)
        {
            return NotFound();
        }

        Response.Headers.CacheControl = "public, max-age=3600";
        return File(image.Bytes, image.ContentType);
    }
}
