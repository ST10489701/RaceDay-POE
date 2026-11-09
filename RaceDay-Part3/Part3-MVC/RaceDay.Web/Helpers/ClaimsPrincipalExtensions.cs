using System.Security.Claims;

namespace RaceDay.Web.Helpers;

public static class ClaimsPrincipalExtensions
{
    /// <summary>The logged-in user's id (stored in the login cookie), or 0 when nobody is logged in.</summary>
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(value, out var id) ? id : 0;
    }
}
