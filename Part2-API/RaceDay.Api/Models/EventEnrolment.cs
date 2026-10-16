namespace RaceDay.Api.Models;

/// <summary>Maps to the EventEnrolments table. Links a Participant to the Category they entered.</summary>
public class EventEnrolment
{
    public int EnrolmentId { get; set; }
    public int ParticipantId { get; set; }
    public int CategoryId { get; set; }
    public string? BibNumber { get; set; }
    public DateTime EnrolmentDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = EnrolmentStatuses.Confirmed;

    public User Participant { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public RaceResult? Result { get; set; }
}
