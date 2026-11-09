namespace RaceDay.Web.Helpers;

/// <summary>Small display helpers used by the Razor views.</summary>
public static class ViewHelpers
{
    /// <summary>Address of a stored image on this site. The site fetches it from the API.</summary>
    public static string? MediaUrl(string? storedPath) =>
        string.IsNullOrWhiteSpace(storedPath) ? null : "/media/" + storedPath.TrimStart('/');

    /// <summary>Bootstrap badge colour for a status: green = good, amber = waiting, red = cancelled/disqualified.</summary>
    public static string StatusClass(string? status) => status switch
    {
        "Confirmed" => "bg-success",
        "Finished" => "bg-success",
        "Pending" => "bg-warning text-dark",
        "DNF" => "bg-warning text-dark",
        "Cancelled" => "bg-danger",
        "DQ" => "bg-danger",
        _ => "bg-secondary"
    };

    /// <summary>47 becomes "47th", 1 becomes "1st", 12 becomes "12th".</summary>
    public static string Ordinal(int number)
    {
        var lastTwo = number % 100;
        if (lastTwo is >= 11 and <= 13)
        {
            return number + "th";
        }

        return (number % 10) switch
        {
            1 => number + "st",
            2 => number + "nd",
            3 => number + "rd",
            _ => number + "th"
        };
    }

    public static string FormatTime(TimeSpan? time) => time is null ? "-" : time.Value.ToString(@"hh\:mm\:ss");

    public static string FormatDate(DateOnly date) => date.ToString("ddd, dd MMM yyyy");

    public static string TypeIcon(string? eventType) => eventType switch
    {
        "Run" => "bi-lightning-charge-fill",
        "Walk" => "bi-person-walking",
        "Cycle" => "bi-bicycle",
        _ => "bi-flag-fill"
    };
}
