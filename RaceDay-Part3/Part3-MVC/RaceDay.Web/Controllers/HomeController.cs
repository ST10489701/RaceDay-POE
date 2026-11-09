using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

public class HomeController : Controller
{
    private readonly IRaceDayApiClient _api;

    public HomeController(IRaceDayApiClient api)
    {
        _api = api;
    }

    /// <summary>Participant home page: upcoming events with filtering. Organisers are sent to their dashboard.</summary>
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? type, string? search, bool upcomingOnly = true)
    {
        if (User.IsInRole(AppRoles.Organiser))
        {
            return RedirectToAction("Dashboard", "Organiser");
        }

        var events = await _api.GetEventsAsync(type, upcomingOnly);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            events = events
                .Where(e => e.EventName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                            (e.Location ?? string.Empty).Contains(term, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return View(new HomeIndexViewModel { Events = events, Type = type, Search = search, UpcomingOnly = upcomingOnly });
    }

    /// <summary>Friendly page for errors such as 404 (not found) and 503 (service unavailable).</summary>
    [AllowAnonymous]
    public IActionResult Status(int code = 500)
    {
        Response.StatusCode = code;

        var model = code switch
        {
            404 => new StatusViewModel { Code = 404, Title = "We could not find that page", Message = "The page or event you were looking for does not exist, or it has been removed." },
            503 => new StatusViewModel { Code = 503, Title = "RaceDay is taking a short break", Message = "We could not reach the RaceDay service just now. Please try again in a moment." },
            403 => new StatusViewModel { Code = 403, Title = "Access denied", Message = "You do not have permission to see that page." },
            _ => new StatusViewModel { Code = code, Title = "Something went wrong", Message = "Something unexpected happened. Please try again." }
        };

        return View("Status", model);
    }

    /// <summary>Shown when an unexpected error happens. It never shows technical details to the user.</summary>
    [AllowAnonymous]
    public IActionResult Error()
    {
        Response.StatusCode = 500;
        return View("Status", new StatusViewModel
        {
            Code = 500,
            Title = "Something went wrong",
            Message = "Sorry, something unexpected happened on our side. Please try again."
        });
    }
}
