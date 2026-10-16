using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>
/// Proves role-based access: logged-out users get 401, and the wrong role gets 403,
/// before the request is even looked at (so the ids/bodies used here do not matter).
/// </summary>
public class RoleAccessTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public RoleAccessTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    public static IEnumerable<object[]> OrganiserOnlyEndpoints => new List<object[]>
    {
        new object[] { "POST", "/api/events" },
        new object[] { "PUT", "/api/events/1" },
        new object[] { "DELETE", "/api/events/1" },
        new object[] { "POST", "/api/events/1/categories" },
        new object[] { "PUT", "/api/categories/1" },
        new object[] { "DELETE", "/api/categories/1" },
        new object[] { "GET", "/api/events/1/enrolments" },
        new object[] { "POST", "/api/results" }
    };

    public static IEnumerable<object[]> ParticipantOnlyEndpoints => new List<object[]>
    {
        new object[] { "POST", "/api/events/1/enrolments" },
        new object[] { "GET", "/api/enrolments/my" },
        new object[] { "DELETE", "/api/enrolments/1" }
    };

    public static IEnumerable<object[]> AnyLoggedInEndpoints => new List<object[]>
    {
        new object[] { "GET", "/api/users/1" },
        new object[] { "PUT", "/api/users/1" },
        new object[] { "POST", "/api/auth/logout" },
        new object[] { "GET", "/api/results/1" },
        new object[] { "GET", "/api/users/1/results" }
    };

    public static IEnumerable<object[]> AllProtectedEndpoints =>
        OrganiserOnlyEndpoints.Concat(ParticipantOnlyEndpoints).Concat(AnyLoggedInEndpoints);

    private static HttpRequestMessage Build(string method, string url)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), url);
        if (method == "POST" || method == "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }

        return request;
    }

    [Theory]
    [MemberData(nameof(OrganiserOnlyEndpoints))]
    public async Task Participant_CannotUseOrganiserEndpoints_Returns403(string method, string url)
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var response = await client.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ParticipantOnlyEndpoints))]
    public async Task Organiser_CannotUseParticipantEndpoints_Returns403(string method, string url)
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Organiser1Email);

        var response = await client.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllProtectedEndpoints))]
    public async Task LoggedOutUser_CannotUseProtectedEndpoints_Returns401(string method, string url)
    {
        var client = _factory.CreateApiClient();

        var response = await client.SendAsync(Build(method, url));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/events")]
    [InlineData("/api/events/1")]
    [InlineData("/api/events/1/categories")]
    [InlineData("/api/events/1/routes")]
    public async Task PublicEndpoints_AreAvailableWithoutLogin(string url)
    {
        var response = await _factory.CreateApiClient().GetAsync(url);

        // 200 for the list, 404 when the event id does not exist - but never 401/403.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
