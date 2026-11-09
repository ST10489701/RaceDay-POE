using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password."), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address."), EmailAddress(ErrorMessage = "That does not look like a valid email address."), StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That does not look like a valid phone number."), StringLength(20)]
    [Display(Name = "Phone number (optional)")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Please choose a password."), StringLength(100, MinimumLength = 8, ErrorMessage = "The password must be at least 8 characters long.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please repeat your password."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a role.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = "Participant";

    public static IEnumerable<SelectListItem> RoleOptions => new[]
    {
        new SelectListItem("Participant (I want to enter events)", "Participant"),
        new SelectListItem("Organiser (I run events)", "Organiser")
    };
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name."), StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "That does not look like a valid phone number."), StringLength(20)]
    [Display(Name = "Phone number")]
    public string? PhoneNumber { get; set; }

    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
}

public class HomeIndexViewModel
{
    public List<EventDto> Events { get; set; } = new();
    public string? Type { get; set; }
    public string? Search { get; set; }
    public bool UpcomingOnly { get; set; } = true;
}

public class EventDetailsViewModel
{
    public EventDetailDto Event { get; set; } = new();

    /// <summary>The logged-in participant's confirmed enrolment in this event, if any.</summary>
    public EnrolmentDto? MyEnrolment { get; set; }
}

public class DashboardRow
{
    public EventDto Event { get; set; } = new();
    public int Enrolments { get; set; }
}

public class DashboardViewModel
{
    public List<DashboardRow> Rows { get; set; } = new();
    public int TotalEvents => Rows.Count;
    public int TotalEnrolments => Rows.Sum(r => r.Enrolments);
    public int UpcomingEvents { get; set; }
}

public class EventFormViewModel
{
    public int? EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name."), StringLength(150)]
    [Display(Name = "Event name")]
    public string EventName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter a description."), StringLength(4000)]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the event date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Date")]
    public DateOnly? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location."), StringLength(150)]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the distance in kilometres."), Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose an event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    [Display(Name = "Banner image")]
    public IFormFile? Banner { get; set; }

    public string? ExistingBanner { get; set; }

    public static IEnumerable<SelectListItem> TypeOptions => new[]
    {
        new SelectListItem("Run", "Run"),
        new SelectListItem("Walk", "Walk"),
        new SelectListItem("Cycle", "Cycle")
    };

    public EventRequestDto ToRequest() => new()
    {
        EventName = EventName.Trim(),
        Description = Description.Trim(),
        EventDate = EventDate,
        Location = Location.Trim(),
        DistanceKm = DistanceKm,
        EventType = EventType
    };
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter the category name (for example Under 20 or 10km)."), StringLength(100)]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.01, 999.99, ErrorMessage = "The distance must be between 0.01 and 999.99 km.")]
    [Display(Name = "Distance (km) - leave empty for age categories")]
    public decimal? DistanceKm { get; set; }

    [Range(0, 999999.99, ErrorMessage = "The entry fee must be 0 or more.")]
    [Display(Name = "Entry fee (R)")]
    public decimal EntryFee { get; set; }

    [Range(1, 1000000, ErrorMessage = "The limit must be at least 1.")]
    [Display(Name = "Maximum participants (optional)")]
    public int? MaxParticipants { get; set; }

    public CategoryRequestDto ToRequest() => new()
    {
        CategoryName = CategoryName.Trim(),
        DistanceKm = DistanceKm,
        EntryFee = EntryFee,
        MaxParticipants = MaxParticipants
    };
}

public class CategoriesViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EnrolmentsViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public List<EnrolmentDto> Enrolments { get; set; } = new();
}

public class ResultRow
{
    public EnrolmentDto Enrolment { get; set; } = new();
    public ResultDto? Result { get; set; }
}

public class ResultsViewModel
{
    public EventDetailDto Event { get; set; } = new();
    public List<ResultRow> Rows { get; set; } = new();
    public bool EventHasStarted { get; set; }
}

public class CaptureResultForm
{
    public int EventId { get; set; }
    public int EnrolmentId { get; set; }
    public string Status { get; set; } = "Finished";
    public string? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class EventPickerViewModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetAction { get; set; } = string.Empty;
    public List<EventDto> Events { get; set; } = new();
}

public class StatusViewModel
{
    public int Code { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
