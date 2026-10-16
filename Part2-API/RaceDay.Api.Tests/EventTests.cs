using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Event and category management by Organisers.</summary>
public class EventTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public EventTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static EventRequest NewEventRequest(int daysAhead = 30) => new()
    {
        EventName = "Test Fun Run",
        Description = "A test event",
        EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(daysAhead)),
        Location = "Johannesburg",
        DistanceKm = 10m,
        EventType = "Run"
    };

    [Fact]
    public async Task GetEvents_WithoutLogin_Returns200_AndListsEvents()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();

        var response = await _factory.CreateApiClient().GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var events = await response.Content.ReadFromJsonAsync<List<EventResponse>>();
        Assert.Contains(events!, e => e.EventId == seeded.EventId);
    }

    [Fact]
    public async Task GetEvents_WithUnknownEventType_Returns400()
    {
        var response = await _factory.CreateApiClient().GetAsync("/api/events?eventType=Swim");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetEvent_ById_ReturnsCategories()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();

        var response = await _factory.CreateApiClient().GetAsync($"/api/events/{seeded.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<EventDetailResponse>();
        Assert.Single(detail!.Categories);
    }

    [Fact]
    public async Task GetEvent_WithUnknownId_Returns404()
    {
        var response = await _factory.CreateApiClient().GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_Returns201_AndOrganiserOwnsIt()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PostAsJsonAsync("/api/events", NewEventRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.Equal(await _factory.GetUserIdAsync(RaceDayFactory.Organiser1Email), created!.OrganiserId);
        Assert.Equal("Run", created.EventType);
        Assert.Equal(10m, created.DistanceKm);
    }

    [Fact]
    public async Task CreateEvent_WithPastDate_Returns400()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PostAsJsonAsync("/api/events", NewEventRequest(daysAhead: -5));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithInvalidEventType_Returns400()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        var request = NewEventRequest();
        request.EventType = "Swim";

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithMissingName_Returns400()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        var request = NewEventRequest();
        request.EventName = "";

        var response = await client.PostAsJsonAsync("/api/events", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_AsOwner_Returns200_AndSavesChanges()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        var request = NewEventRequest();
        request.EventName = "Renamed Event";
        request.EventType = "Cycle";

        var response = await client.PutAsJsonAsync($"/api/events/{seeded.EventId}", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stored = await _factory.QueryDbAsync(db => db.Events.SingleAsync(e => e.EventId == seeded.EventId));
        Assert.Equal("Renamed Event", stored.EventName);
        Assert.Equal("Cycle", stored.EventType);
    }

    [Fact]
    public async Task UpdateEvent_OwnedByAnotherOrganiser_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(RaceDayFactory.Organiser1Email);
        var otherOrganiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await otherOrganiser.PutAsJsonAsync($"/api/events/{seeded.EventId}", NewEventRequest());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateEvent_WithUnknownId_Returns404()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PutAsJsonAsync("/api/events/999999", NewEventRequest());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_AsOwner_Returns200_AndEventIsGone()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.DeleteAsync($"/api/events/{seeded.EventId}");
        var afterwards = await client.GetAsync($"/api/events/{seeded.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterwards.StatusCode);
    }

    [Fact]
    public async Task DeleteEvent_RemovesItsEnrolmentsToo()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.DeleteAsync($"/api/events/{seeded.EventId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var enrolmentStillExists = await _factory.QueryDbAsync(db => db.EventEnrolments.AnyAsync(e => e.EnrolmentId == seeded.EnrolmentId));
        Assert.False(enrolmentStillExists);
    }

    [Fact]
    public async Task DeleteEvent_OwnedByAnotherOrganiser_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(RaceDayFactory.Organiser1Email);
        var otherOrganiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await otherOrganiser.DeleteAsync($"/api/events/{seeded.EventId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsOwner_Returns201_AndAgeCategoryNeedsNoDistance()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PostAsJsonAsync($"/api/events/{seeded.EventId}/categories",
            new CategoryRequest { CategoryName = "Under 20", DistanceKm = null, EntryFee = 0m });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Null(category!.DistanceKm);
        Assert.Equal(seeded.EventId, category.EventId);
    }

    [Fact]
    public async Task CreateCategory_ForAnotherOrganisersEvent_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(RaceDayFactory.Organiser1Email);
        var otherOrganiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{seeded.EventId}/categories",
            new CategoryRequest { CategoryName = "Senior", EntryFee = 50m });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_WithoutLogin_ReturnsCategories()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();

        var response = await _factory.CreateApiClient().GetAsync($"/api/events/{seeded.EventId}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var categories = await response.Content.ReadFromJsonAsync<List<CategoryResponse>>();
        Assert.Single(categories!);
    }

    [Fact]
    public async Task UpdateCategory_AsOwner_Returns200()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PutAsJsonAsync($"/api/categories/{seeded.CategoryId}",
            new CategoryRequest { CategoryName = "10km Senior", DistanceKm = 10m, EntryFee = 120m, MaxParticipants = 50 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryResponse>();
        Assert.Equal("10km Senior", category!.CategoryName);
    }

    [Fact]
    public async Task DeleteCategory_WithEnrolments_Returns409()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.DeleteAsync($"/api/categories/{seeded.CategoryId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_WithoutEnrolments_Returns200()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.DeleteAsync($"/api/categories/{seeded.CategoryId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
