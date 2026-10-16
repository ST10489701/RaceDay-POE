using System.Security.Claims;

namespace RaceDay.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Returns the logged-in user's id from the session cookie, or 0 if there is none.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}
