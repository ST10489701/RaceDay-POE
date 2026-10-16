using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>A result an Organiser captures for one enrolment.</summary>
public class CaptureResultRequest
{
    [Range(1, int.MaxValue)]
    public int EnrolmentId { get; set; }

    /// <summary>Format hh:mm:ss, for example 01:45:32. Required when Status is "Finished".</summary>
    public TimeSpan? FinishTime { get; set; }

    /// <summary>Finishing position (1 or higher). Required when Status is "Finished".</summary>
    [Range(1, 1000000)]
    public int? Position { get; set; }

    /// <summary>One of "Finished", "DNF" or "DQ".</summary>
    [Required]
    public string Status { get; set; } = ResultStatuses.Finished;
}

/// <summary>A result as returned by the API.</summary>
public class ResultResponse
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public string Status { get; set; } = string.Empty;

    public static readonly Expression<Func<RaceResult, ResultResponse>> Projection = r => new ResultResponse
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        ParticipantId = r.Enrolment.ParticipantId,
        ParticipantName = r.Enrolment.Participant.FullName,
        EventId = r.Enrolment.Category.EventId,
        EventName = r.Enrolment.Category.Event.EventName,
        EventDate = r.Enrolment.Category.Event.EventDate,
        CategoryName = r.Enrolment.Category.CategoryName,
        FinishTime = r.FinishTime,
        Position = r.Position,
        Status = r.Status
    };
}
