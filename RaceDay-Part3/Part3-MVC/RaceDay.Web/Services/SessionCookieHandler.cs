namespace RaceDay.Web.Services;

/// <summary>
/// Keeps the user's API session. When the API logs someone in it sends a session cookie; this handler
/// stores that cookie in the MVC server-side session and sends it back with every later API call,
/// so the API always knows who the user is and what role they have.
/// </summary>
public class SessionCookieHandler : DelegatingHandler
{
    public const string ApiCookieName = "RaceDay.Session";
    private const string SessionKey = "ApiSessionCookie";

    private readonly IHttpContextAccessor _accessor;

    public SessionCookieHandler(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var session = _accessor.HttpContext?.Session;

        var stored = session?.GetString(SessionKey);
        if (!string.IsNullOrEmpty(stored))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={stored}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        if (session is not null && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies)
            {
                var value = ExtractValue(setCookie);
                if (value is null)
                {
                    continue;
                }

                if (value.Length == 0)
                {
                    session.Remove(SessionKey);
                }
                else
                {
                    session.SetString(SessionKey, value);
                }
            }
        }

        return response;
    }

    private static string? ExtractValue(string setCookie)
    {
        var firstPart = setCookie.Split(';')[0].Trim();
        var prefix = ApiCookieName + "=";
        return firstPart.StartsWith(prefix, StringComparison.Ordinal) ? firstPart[prefix.Length..] : null;
    }
}
