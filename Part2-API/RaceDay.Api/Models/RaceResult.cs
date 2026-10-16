namespace RaceDay.Api.Models;

/// <summary>Maps to the Results table. At most one result per enrolment (one-to-one).</summary>
public class RaceResult
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = ResultStatuses.Finished;

    public EventEnrolment Enrolment { get; set; } = null!;
}
