using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;
using RaceDay.Web.ViewModels;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Pages only Organisers can see. All rules (who owns an event, what is allowed) are enforced by the API;
/// this controller only collects input and shows what the API returns.
/// </summary>
[Authorize(Roles = AppRoles.Organiser)]
public class OrganiserController : Controller
{
    private static readonly Regex TimePattern = new(@"^\d{1,2}:\d{2}:\d{2}$", RegexOptions.Compiled);

    private readonly IRaceDayApiClient _api;

    public OrganiserController(IRaceDayApiClient api)
    {
        _api = api;
    }

    // ---------- Dashboard ----------
    public async Task<IActionResult> Dashboard()
    {
        var events = await GetMyEventsAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);

        var model = new DashboardViewModel { UpcomingEvents = events.Count(e => e.EventDate >= today) };
        foreach (var ev in events)
        {
            var enrolments = await _api.GetEventEnrolmentsAsync(ev.EventId);
            model.Rows.Add(new DashboardRow { Event = ev, Enrolments = enrolments.Count(e => e.Status == "Confirmed") });
        }

        return View(model);
    }

    // ---------- Events management ----------
    public async Task<IActionResult> Events() => View(await GetMyEventsAsync());

    [HttpGet]
    public IActionResult CreateEvent() =>
        View("EventForm", new EventFormViewModel { EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)) });

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        try
        {
            var created = await _api.CreateEventAsync(model.ToRequest());
            return await FinishEventSaveAsync(created.EventId, model, "Event created.");
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 409)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("EventForm", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var ev = await _api.GetEventAsync(id);
        return View("EventForm", new EventFormViewModel
        {
            EventId = ev.EventId,
            EventName = ev.EventName,
            Description = ev.Description ?? string.Empty,
            EventDate = ev.EventDate,
            Location = ev.Location ?? string.Empty,
            DistanceKm = ev.DistanceKm,
            EventType = ev.EventType,
            ExistingBanner = ev.BannerImage
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventId = id;
        if (!ModelState.IsValid)
        {
            return View("EventForm", model);
        }

        try
        {
            await _api.UpdateEventAsync(id, model.ToRequest());
            return await FinishEventSaveAsync(id, model, "Event updated.");
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 409)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("EventForm", model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        await _api.DeleteEventAsync(id);
        TempData["Success"] = "Event deleted.";
        return RedirectToAction(nameof(Events));
    }

    // ---------- Categories ----------
    public async Task<IActionResult> Categories(int? eventId)
    {
        if (eventId is null)
        {
            return View("EventPicker", await PickerAsync("Manage categories", "Choose an event to add or change its age and distance categories.", nameof(Categories)));
        }

        var ev = await _api.GetEventAsync(eventId.Value);
        return View(new CategoriesViewModel { Event = ev, NewCategory = new CategoryFormViewModel { EventId = ev.EventId } });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = FirstError();
            return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
        }

        await _api.CreateCategoryAsync(model.EventId, model.ToRequest());
        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int eventId, int id)
    {
        var categories = await _api.GetCategoriesAsync(eventId);
        var category = categories.FirstOrDefault(c => c.CategoryId == id);
        if (category is null)
        {
            return NotFound();
        }

        return View(new CategoryFormViewModel
        {
            EventId = eventId,
            CategoryId = id,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            EntryFee = category.EntryFee,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(int eventId, int id, CategoryFormViewModel model)
    {
        model.EventId = eventId;
        model.CategoryId = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _api.UpdateCategoryAsync(id, model.ToRequest());
            TempData["Success"] = "Category updated.";
            return RedirectToAction(nameof(Categories), new { eventId });
        }
        catch (ApiException ex) when (ex.StatusCode is 400 or 409)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        await _api.DeleteCategoryAsync(id);
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // ---------- Enrolments ----------
    public async Task<IActionResult> Enrolments(int? eventId)
    {
        if (eventId is null)
        {
            return View("EventPicker", await PickerAsync("Enrolments", "Choose an event to see who has entered.", nameof(Enrolments)));
        }

        var ev = await _api.GetEventAsync(eventId.Value);
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId.Value);
        return View(new EnrolmentsViewModel { Event = ev, Enrolments = enrolments });
    }

    // ---------- Results ----------
    public async Task<IActionResult> Results(int? eventId)
    {
        if (eventId is null)
        {
            return View("EventPicker", await PickerAsync("Capture results", "Choose an event to enter finish times and positions.", nameof(Results)));
        }

        var ev = await _api.GetEventAsync(eventId.Value);
        var enrolments = await _api.GetEventEnrolmentsAsync(eventId.Value);
        var results = await _api.GetEventResultsAsync(eventId.Value);

        var model = new ResultsViewModel
        {
            Event = ev,
            EventHasStarted = ev.EventDate <= DateOnly.FromDateTime(DateTime.Today),
            Rows = enrolments
                .Where(e => e.Status == "Confirmed")
                .Select(e => new ResultRow { Enrolment = e, Result = results.FirstOrDefault(r => r.EnrolmentId == e.EnrolmentId) })
                .ToList()
        };

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CaptureResult(CaptureResultForm form)
    {
        TimeSpan? finishTime = null;
        if (form.Status == "Finished")
        {
            if (string.IsNullOrWhiteSpace(form.FinishTime) || !TimePattern.IsMatch(form.FinishTime.Trim()) || !TimeSpan.TryParse(form.FinishTime.Trim(), out var parsed))
            {
                TempData["Error"] = "Please enter the finish time as hours:minutes:seconds, for example 01:45:32.";
                return RedirectToAction(nameof(Results), new { eventId = form.EventId });
            }

            finishTime = parsed;
        }

        await _api.CaptureResultAsync(new CaptureResultRequestDto
        {
            EnrolmentId = form.EnrolmentId,
            Status = form.Status,
            FinishTime = finishTime,
            Position = form.Status == "Finished" ? form.Position : null
        });

        TempData["Success"] = "Result saved and published to the participant.";
        return RedirectToAction(nameof(Results), new { eventId = form.EventId });
    }

    // ---------- Helpers ----------
    /// <summary>The API lists all events; show only the ones this Organiser created.</summary>
    private async Task<List<EventDto>> GetMyEventsAsync()
    {
        var all = await _api.GetEventsAsync(null, false);
        var me = User.GetUserId();
        return all.Where(e => e.OrganiserId == me).OrderBy(e => e.EventDate).ToList();
    }

    private async Task<EventPickerViewModel> PickerAsync(string title, string description, string targetAction) => new()
    {
        Title = title,
        Description = description,
        TargetAction = targetAction,
        Events = await GetMyEventsAsync()
    };

    /// <summary>After saving an event, upload the banner (if one was chosen) through the API.</summary>
    private async Task<IActionResult> FinishEventSaveAsync(int eventId, EventFormViewModel model, string successMessage)
    {
        if (model.Banner is { Length: > 0 })
        {
            try
            {
                await _api.UploadBannerAsync(eventId, model.Banner);
            }
            catch (ApiException ex) when (ex.StatusCode != StatusCodes.Status401Unauthorized)
            {
                TempData["Error"] = $"The event was saved, but the banner could not be uploaded: {ex.Message}";
                return RedirectToAction(nameof(Events));
            }
        }

        TempData["Success"] = successMessage;
        return RedirectToAction(nameof(Events));
    }

    private string FirstError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m))
        ?? "Please check the details and try again.";
}
