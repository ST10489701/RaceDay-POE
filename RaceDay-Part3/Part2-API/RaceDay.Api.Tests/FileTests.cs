using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Image uploads (Azure Blob Storage is replaced by an in-memory fake) and the extra Part 3 endpoints.</summary>
public class FileTests : IClassFixture<RaceDayFactory>
{
    // The first bytes of a real PNG file. The API checks these "magic numbers", not the file name.
    private static readonly byte[] PngBytes = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };

    private readonly RaceDayFactory _factory;

    public FileTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static MultipartFormDataContent FileContent(byte[] bytes, string fileName = "image.png")
    {
        var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(file, "file", fileName);
        return content;
    }

    [Fact]
    public async Task UploadBanner_AsOwningOrganiser_Returns200_AndImageCanBeDownloaded()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.PostAsync($"/api/events/{seeded.EventId}/banner", FileContent(PngBytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<EventResponse>();
        Assert.StartsWith("banners/", updated!.BannerImage);

        var image = await _factory.CreateApiClient().GetAsync($"/api/images/{updated.BannerImage}");
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal("image/png", image.Content.Headers.ContentType?.MediaType);
        Assert.Equal(PngBytes, await image.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task UploadBanner_WithANonImageFile_Returns400()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        var notAnImage = System.Text.Encoding.UTF8.GetBytes("this is plain text, not a picture");

        var response = await client.PostAsync($"/api/events/{seeded.EventId}/banner", FileContent(notAnImage, "fake.png"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_ForAnotherOrganisersEvent_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(RaceDayFactory.Organiser1Email);
        var otherOrganiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await otherOrganiser.PostAsync($"/api/events/{seeded.EventId}/banner", FileContent(PngBytes));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_AsParticipant_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();
        var participant = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await participant.PostAsync($"/api/events/{seeded.EventId}/banner", FileContent(PngBytes));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WithoutLogin_Returns401()
    {
        var seeded = await _factory.SeedUpcomingEventAsync();

        var response = await _factory.CreateApiClient().PostAsync($"/api/events/{seeded.EventId}/banner", FileContent(PngBytes));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UploadProfilePicture_ForOwnAccount_Returns200()
    {
        var id = await _factory.GetUserIdAsync(RaceDayFactory.Participant1Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.PostAsync($"/api/users/{id}/profile-picture", FileContent(PngBytes));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.StartsWith("profiles/", user!.ProfilePicture);
    }

    [Fact]
    public async Task UploadProfilePicture_ForAnotherUser_Returns403()
    {
        var otherId = await _factory.GetUserIdAsync(RaceDayFactory.Participant2Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.PostAsync($"/api/users/{otherId}/profile-picture", FileContent(PngBytes));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/images/secrets/file.png")]
    [InlineData("/api/images/banners/does-not-exist.png")]
    public async Task GetImage_ForUnknownOrNotAllowedPath_Returns404(string url)
    {
        var response = await _factory.CreateApiClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EventResults_AsOwningOrganiser_ListsResults_WithTotalFinishers()
    {
        var seeded = await _factory.SeedPastEventWithEnrolmentAsync();
        var organiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);
        await organiser.PostAsJsonAsync("/api/results", new CaptureResultRequest
        {
            EnrolmentId = seeded.EnrolmentId,
            FinishTime = new TimeSpan(0, 55, 10),
            Position = 1,
            Status = "Finished"
        });

        var response = await organiser.GetAsync($"/api/events/{seeded.EventId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var results = await response.Content.ReadFromJsonAsync<List<ResultResponse>>();
        var result = Assert.Single(results!);
        Assert.Equal(1, result.TotalFinishers);
    }

    [Fact]
    public async Task EventResults_ForAnotherOrganisersEvent_Returns403()
    {
        var seeded = await _factory.SeedUpcomingEventAsync(RaceDayFactory.Organiser1Email);
        var otherOrganiser = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await otherOrganiser.GetAsync($"/api/events/{seeded.EventId}/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
