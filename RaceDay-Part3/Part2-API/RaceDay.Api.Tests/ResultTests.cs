using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Capturing and viewing results.</summary>
public class ResultTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ResultTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static CaptureResultRequest FinishedResult(int enrolmentId, int position = 5) => new()
    {
        EnrolmentId = enrolmentId,
        FinishTime = new TimeSpan(1, 45, 32),
        Position = position,
        Status = "Finished"
    };

    [Fact]
    public async Task CaptureResult_AsOwningOrganiser_Returns201()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PostAsJsonAsync("/api/results", FinishedResult(seeded.EnrolmentId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(5, result!.Position);
        Assert.Equal(new TimeSpan(1, 45, 32), result.FinishTime);
        Assert.Equal("Finished", result.Status);
    }

    [Fact]
    public async Task CaptureResult_Twice_Returns409()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        await client.PostAsJsonAsync("/api/results", FinishedResult(seeded.EnrolmentId));

        var second = await client.PostAsJsonAsync("/api/results", FinishedResult(seeded.EnrolmentId, position: 6));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForAnotherOrganisersEvent_Returns403()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync(RaceDayFactory.Organiser1Email);
        var otherOrganiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await otherOrganiser.PostAsJsonAsync("/api/results", FinishedResult(seeded.EnrolmentId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_ForUnknownEnrolment_Returns404()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PostAsJsonAsync("/api/results", FinishedResult(999999));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_BeforeTheEventHasHappened_Returns400()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var participant = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);
        var enrolment = await (await participant.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", new EnrolRequest { CategoryId = seeded.CategoryId }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();
        var organiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await organiser.PostAsJsonAsync("/api/results", FinishedResult(enrolment!.EnrolmentId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_FinishedWithoutFinishTime_Returns400()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        var request = FinishedResult(seeded.EnrolmentId);
        request.FinishTime = null;

        var response = await client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_WithInvalidStatus_Returns400()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        var request = FinishedResult(seeded.EnrolmentId);
        request.Status = "Winner";

        var response = await client.PostAsJsonAsync("/api/results", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CaptureResult_AsDnf_Returns201_WithoutTimeOrPosition()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PostAsJsonAsync("/api/results", new CaptureResultRequest { EnrolmentId = seeded.EnrolmentId, Status = "DNF" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Null(result!.FinishTime);
        Assert.Null(result.Position);
    }

    [Fact]
    public async Task Participant_CanViewTheirOwnResult()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync(participantEmail: RaceDayFactory.Participant1Email);
        var organiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        await organiser.PostAsJsonAsync("/api/results", FinishedResult(seeded.EnrolmentId));
        var participant = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await participant.GetAsync($"/api/results/{seeded.EnrolmentId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ResultResponse>();
        Assert.Equal(5, result!.Position);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherParticipantsResult_Returns403()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync(participantEmail: RaceDayFactory.Participant1Email);
        var organiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        await organiser.PostAsJsonAsync("/api/results", FinishedResult(seeded.EnrolmentId));
        var otherParticipant = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant2Email);

        var response = await otherParticipant.GetAsync($"/api/results/{seeded.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Participant_CanViewTheirOwnResultHistory()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync(participantEmail: RaceDayFactory.Participant1Email);
        var organiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        await organiser.PostAsJsonAsync("/api/results", FinishedResult(seeded.EnrolmentId));
        var participantId = await _factory.GetUserIdAsync(RaceDayFactory.Participant1Email);
        var participant = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var history = await participant.GetFromJsonAsync<List<ResultResponse>>($"/api/users/{participantId}/results");

        Assert.Contains(history!, r => r.EnrolmentId == seeded.EnrolmentId);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherUsersResultHistory_Returns403()
    {
        var participant1Id = await _factory.GetUserIdAsync(RaceDayFactory.Participant1Email);
        var participant2 = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant2Email);

        var response = await participant2.GetAsync($"/api/users/{participant1Id}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
