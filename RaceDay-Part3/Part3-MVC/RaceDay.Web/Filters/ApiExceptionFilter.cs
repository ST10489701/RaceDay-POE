using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Services;

namespace RaceDay.Web.Filters;

/// <summary>
/// Catches errors coming back from the API so users see a friendly message or page, never a crash.
/// 401 = session expired (log in again), 403 = access denied page, 404/503 = status page,
/// anything else (for example "category is full") = message shown on the page the user came from.
/// </summary>
public class ApiExceptionFilter : IAsyncExceptionFilter
{
    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiException exception)
        {
            return;
        }

        context.ExceptionHandled = true;
        var http = context.HttpContext;
        var tempData = http.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(http);

        switch (exception.StatusCode)
        {
            case StatusCodes.Status401Unauthorized:
                http.Session.Clear();
                await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                tempData["Error"] = "Your session has ended. Please log in again.";
                context.Result = new RedirectToActionResult("Login", "Account", null);
                break;

            case StatusCodes.Status403Forbidden:
                context.Result = new RedirectToActionResult("AccessDenied", "Account", null);
                break;

            case StatusCodes.Status404NotFound:
                context.Result = new RedirectToActionResult("Status", "Home", new { code = 404 });
                break;

            case StatusCodes.Status503ServiceUnavailable:
                context.Result = new RedirectToActionResult("Status", "Home", new { code = 503 });
                break;

            default:
                tempData["Error"] = exception.Message;
                context.Result = new LocalRedirectResult(SafeReturnPath(http));
                break;
        }
    }

    /// <summary>Goes back to the page the user came from, but only if it is on this same site.</summary>
    private static string SafeReturnPath(HttpContext http)
    {
        var referer = http.Request.Headers.Referer.ToString();
        if (Uri.TryCreate(referer, UriKind.Absolute, out var uri) &&
            string.Equals(uri.Authority, http.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
        {
            return uri.PathAndQuery;
        }

        return "/";
    }
}
