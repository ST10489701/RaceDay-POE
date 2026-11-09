using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Participants enrolling, and the enrolment being recorded correctly.</summary>
public class EnrolmentTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EnrolmentTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Enrol_AsParticipant_Returns201_AndRecordsParticipantEventAndCategory()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var participantId = await _factory.GetUserIdAsync(RaceDayFactory.Participant1Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", new EnrolRequest { CategoryId = seeded.CategoryId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var enrolment = await response.Content.ReadFromJsonAsync<EnrolmentResponse>();
        Assert.Equal(participantId, enrolment!.ParticipantId);
        Assert.Equal(seeded.EventId, enrolment.EventId);
        Assert.Equal(seeded.CategoryId, enrolment.CategoryId);
        Assert.Equal("Confirmed", enrolment.Status);
        Assert.False(string.IsNullOrEmpty(enrolment.BibNumber));

        var stored = await _factory.QueryDbAsync(db => db.EventEnrolments
            .SingleAsync(e => e.ParticipantId == participantId && e.CategoryId == seeded.CategoryId));
        Assert.Equal(enrolment.EnrolmentId, stored.EnrolmentId);
    }

    [Fact]
    public async Task Enrol_TwiceInTheSameEvent_Returns409()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);
        var request = new EnrolRequest { CategoryId = seeded.CategoryId };
        await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", request);

        var second = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_WithCategoryFromAnotherEvent_Returns404()
    {
        var eventA = await _factory.SeedUpcomingEventAsync();
        var eventB = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.PostAsJsonAsync($"/api/events/{eventA.EventId}/enrolments", new EnrolRequest { CategoryId = eventB.CategoryId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_InUnknownEvent_Returns404()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.PostAsJsonAsync("/api/events/999999/enrolments", new EnrolRequest { CategoryId = 1 });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Enrol_WhenCategoryIsFull_Returns409()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(maxParticipants: 1);
        var first = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);
        var second = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant2Email);
        var request = new EnrolRequest { CategoryId = seeded.CategoryId };

        var firstResponse = await first.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", request);
        var secondResponse = await second.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", request);

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Enrol_InAnEventThatAlreadyHappened_Returns400()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync(participantEmail: RaceDayFactory.Participant1Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant2Email);

        var response = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", new EnrolRequest { CategoryId = seeded.CategoryId });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task MyEnrolments_ReturnsOnlyTheLoggedInParticipantsEnrolments()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var participant1 = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);
        var participant2 = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant2Email);
        await participant1.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", new EnrolRequest { CategoryId = seeded.CategoryId });

        var mine = await participant1.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/my");
        var theirs = await participant2.GetFromJsonAsync<List<EnrolmentResponse>>("/api/enrolments/my");

        Assert.Contains(mine!, e => e.EventId == seeded.EventId);
        Assert.DoesNotContain(theirs!, e => e.EventId == seeded.EventId);
    }

    [Fact]
    public async Task OrganiserViewsEnrolments_ForTheirOwnEvent_Returns200()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(RaceDayFactory.Organiser1Email);
        var participant = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);
        await participant.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", new EnrolRequest { CategoryId = seeded.CategoryId });
        var organiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await organiser.GetAsync($"/api/events/{seeded.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrolments = await response.Content.ReadFromJsonAsync<List<EnrolmentResponse>>();
        Assert.Single(enrolments!);
    }

    [Fact]
    public async Task OrganiserViewsEnrolments_ForAnotherOrganisersEvent_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(RaceDayFactory.Organiser1Email);
        var otherOrganiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await otherOrganiser.GetAsync($"/api/events/{seeded.EventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_ThenEnrolAgain_Works()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);
        var request = new EnrolRequest { CategoryId = seeded.CategoryId };
        var created = await (await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", request))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();

        var cancel = await client.DeleteAsync($"/api/enrolments/{created!.EnrolmentId}");
        var status = await _factory.QueryDbAsync(db => db.EventEnrolments.Where(e => e.EnrolmentId == created.EnrolmentId).Select(e => e.Status).SingleAsync());
        var again = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", request);

        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        Assert.Equal(EnrolmentStatuses.Cancelled, status);
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
    }

    [Fact]
    public async Task CancelEnrolment_BelongingToAnotherParticipant_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var owner = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);
        var other = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant2Email);
        var created = await (await owner.PostAsJsonAsync($"/api/events/{seeded.EventId}/enrolments", new EnrolRequest { CategoryId = seeded.CategoryId }))
            .Content.ReadFromJsonAsync<EnrolmentResponse>();

        var response = await other.DeleteAsync($"/api/enrolments/{created!.EnrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
