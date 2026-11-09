using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Dtos;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>User registration and login (success and failure cases).</summary>
public class AuthTests : IClassFixture<RaceDayFactory>
{
    private readonly RaceDayFactory _factory;

    public AuthTests(RaceDayFactory factory)
    {
        _factory = factory;
    }

    private static RegisterRequest NewRegistration(string role = "Participant", string password = "StrongPass1!", string? email = null) => new()
    {
        FullName = "New Runner",
        Email = email ?? $"{Guid.NewGuid():N}@raceday.test",
        Password = password,
        PhoneNumber = "0821234567",
        Role = role
    };

    [Fact]
    public async Task Register_AsParticipant_Returns201_AndStoresHashedPassword()
    {
        var client = _factory.CreateApiClient();
        var request = NewRegistration();

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Participant", body!.Role);

        var stored = await _factory.QueryDbAsync(db => db.Users.SingleAsync(u => u.Email == request.Email));
        Assert.NotEqual(request.Password, stored.PasswordHash);
        Assert.DoesNotContain(request.Password, stored.PasswordHash);
    }

    [Fact]
    public async Task Register_AsOrganiser_Returns201()
    {
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/register", NewRegistration("Organiser"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Organiser", body!.Role);
    }

    [Fact]
    public async Task Register_WithInvalidRole_Returns400()
    {
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/register", NewRegistration("Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var client = _factory.CreateApiClient();
        var request = NewRegistration();
        await client.PostAsJsonAsync("/api/auth/register", request);

        var second = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Register_WithShortPassword_Returns400()
    {
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/register", NewRegistration(password: "short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/register", NewRegistration(email: "not-an-email"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_Returns200_AndSetsSessionCookie()
    {
        var client = _factory.CreateApiClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = RaceDayFactory.Organiser1Email, Password = RaceDayFactory.Password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.Equal("Organiser", body!.Role);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        Assert.Contains(cookies!, c => c.StartsWith("RaceDay.Session="));
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = RaceDayFactory.Organiser1Email, Password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var response = await _factory.CreateApiClient().PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = "nobody@raceday.test", Password = RaceDayFactory.Password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutLogin_Returns401()
    {
        var response = await _factory.CreateApiClient().GetAsync("/api/enrolments/my");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Session_IsRemembered_UntilLogout()
    {
        var client = await _factory.CreateApiClient().LoginAsync(RaceDayFactory.Participant1Email);

        var whileLoggedIn = await client.GetAsync("/api/enrolments/my");
        var logout = await client.PostAsync("/api/auth/logout", null);
        var afterLogout = await client.GetAsync("/api/enrolments/my");

        Assert.Equal(HttpStatusCode.OK, whileLoggedIn.StatusCode);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }
}
