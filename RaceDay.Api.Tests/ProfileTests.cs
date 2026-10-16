using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Both roles can view and update their own profile, but not other people's.</summary>
public class ProfileTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public ProfileTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task User_CanViewOwnProfile()
    {
        var id = await _factory.GetUserIdAsync(RaceDayFactory.Participant1Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.GetAsync($"/api/users/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal(RaceDayFactory.Participant1Email, profile!.Email);
    }

    [Fact]
    public async Task Participant_CannotViewAnotherUsersProfile_Returns403()
    {
        var otherId = await _factory.GetUserIdAsync(RaceDayFactory.Participant2Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.GetAsync($"/api/users/{otherId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Organiser_CanViewAParticipantsProfile()
    {
        var participantId = await _factory.GetUserIdAsync(RaceDayFactory.Participant1Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.GetAsync($"/api/users/{participantId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task User_CanUpdateOwnProfile()
    {
        var id = await _factory.GetUserIdAsync(RaceDayFactory.Organiser2Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser2Email);

        var response = await client.PutAsJsonAsync($"/api/users/{id}", new UpdateProfileRequest { FullName = "Updated Name", PhoneNumber = "0831112222" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Updated Name", profile!.FullName);
        Assert.Equal("Organiser", profile.Role);
    }

    [Fact]
    public async Task User_CannotUpdateSomeoneElsesProfile_Returns403()
    {
        var otherId = await _factory.GetUserIdAsync(RaceDayFactory.Participant2Email);
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.PutAsJsonAsync($"/api/users/{otherId}", new UpdateProfileRequest { FullName = "Hacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
