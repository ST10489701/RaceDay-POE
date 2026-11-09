using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>Event details page (anyone can look) and enrolling (Participants only).</summary>
public class EventsController : Controller
{
    private readonly IRaceDayApiClient _api;

    public EventsController(IRaceDayApiClient api)
    {
        _api = api;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var ev = await _api.GetEventAsync(id);

        EnrolmentDto? mine = null;
        if (User.IsInRole(AppRoles.Participant))
        {
            var enrolments = await _api.GetMyEnrolmentsAsync();
            mine = enrolments.FirstOrDefault(e => e.EventId == id && e.Status == "Confirmed");
        }

        return View(new EventDetailsViewModel { Event = ev, MyEnrolment = mine });
    }

    [HttpPost, Authorize(Roles = AppRoles.Participant)]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var enrolment = await _api.EnrolAsync(eventId, categoryId);
        TempData["Success"] = $"You are in! {enrolment.EventName} ({enrolment.CategoryName}) - your bib number is {enrolment.BibNumber}.";
        return RedirectToAction("MyEnrolments", "Participant");
    }
}
