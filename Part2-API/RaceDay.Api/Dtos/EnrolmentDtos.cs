using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Dtos;

/// <summary>The category a participant wants to enter.</summary>
public class EnrolRequest
{
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}

/// <summary>Links a participant, an event and the selected category.</summary>
public class EnrolmentResponse
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateOnly EventDate { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? BibNumber { get; set; }
    public DateTime EnrolmentDate { get; set; }
    public string Status { get; set; } = string.Empty;

    public static readonly Expression<Func<EventEnrolment, EnrolmentResponse>> Projection = e => new EnrolmentResponse
    {
        EnrolmentId = e.EnrolmentId,
        ParticipantId = e.ParticipantId,
        ParticipantName = e.Participant.FullName,
        EventId = e.Category.EventId,
        EventName = e.Category.Event.EventName,
        EventDate = e.Category.Event.EventDate,
        CategoryId = e.CategoryId,
        CategoryName = e.Category.CategoryName,
        BibNumber = e.BibNumber,
        EnrolmentDate = e.EnrolmentDate,
        Status = e.Status
    };
}
