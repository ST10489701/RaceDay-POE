using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Pages only Participants can see: their enrolments and their race history.</summary>
[Authorize(Roles = AppRoles.Participant)]
public class ParticipantController : Controller
{
    private readonly IRaceDayApiClient _api;

    public ParticipantController(IRaceDayApiClient api)
    {
        _api = api;
    }

    public async Task<IActionResult> MyEnrolments()
    {
        var enrolments = await _api.GetMyEnrolmentsAsync();
        return View(enrolments);
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        await _api.CancelEnrolmentAsync(id);
        TempData["Success"] = "Your enrolment has been cancelled.";
        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyResults()
    {
        var results = await _api.GetUserResultsAsync(User.GetUserId());
        return View(results);
    }
}
