namespace RaceDay.Api.Models;

/// <summary>The two user roles supported by RaceDay.</summary>
public static class Roles
{
    public const string Organiser = "Organiser";
    public const string Participant = "Participant";

    public static readonly string[] All = { Organiser, Participant };
}

/// <summary>Allowed values for Event.EventType.</summary>
public static class EventTypes
{
    public const string Run = "Run";
    public const string Walk = "Walk";
    public const string Cycle = "Cycle";

    public static readonly string[] All = { Run, Walk, Cycle };
}

/// <summary>Allowed values for EventEnrolment.Status.</summary>
public static class EnrolmentStatuses
{
    public const string Confirmed = "Confirmed";
    public const string Cancelled = "Cancelled";
}

/// <summary>Allowed values for RaceResult.Status.</summary>
public static class ResultStatuses
{
    public const string Finished = "Finished";
    public const string DNF = "DNF";
    public const string DQ = "DQ";

    public static readonly string[] All = { Finished, DNF, DQ };
}
