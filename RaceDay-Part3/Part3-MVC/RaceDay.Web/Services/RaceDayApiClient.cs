using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Services;

/// <summary>Every call the MVC site makes to the Part 2 API.</summary>
public interface IRaceDayApiClient
{
    Task<UserDto> LoginAsync(string email, string password);
    Task<UserDto> RegisterAsync(string fullName, string email, string password, string? phoneNumber, string role);
    Task LogoutAsync();

    Task<UserDto> GetProfileAsync(int userId);
    Task<UserDto> UpdateProfileAsync(int userId, string fullName, string? phoneNumber);
    Task<UserDto> UploadProfilePictureAsync(int userId, IFormFile file);

    Task<List<EventDto>> GetEventsAsync(string? eventType, bool upcomingOnly);
    Task<EventDetailDto> GetEventAsync(int eventId);
    Task<EventDto> CreateEventAsync(EventRequestDto request);
    Task<EventDto> UpdateEventAsync(int eventId, EventRequestDto request);
    Task DeleteEventAsync(int eventId);
    Task<EventDto> UploadBannerAsync(int eventId, IFormFile file);

    Task<List<CategoryDto>> GetCategoriesAsync(int eventId);
    Task<CategoryDto> CreateCategoryAsync(int eventId, CategoryRequestDto request);
    Task<CategoryDto> UpdateCategoryAsync(int categoryId, CategoryRequestDto request);
    Task DeleteCategoryAsync(int categoryId);

    Task<EnrolmentDto> EnrolAsync(int eventId, int categoryId);
    Task<List<EnrolmentDto>> GetMyEnrolmentsAsync();
    Task<List<EnrolmentDto>> GetEventEnrolmentsAsync(int eventId);
    Task CancelEnrolmentAsync(int enrolmentId);

    Task<ResultDto> CaptureResultAsync(CaptureResultRequestDto request);
    Task<List<ResultDto>> GetUserResultsAsync(int userId);
    Task<List<ResultDto>> GetEventResultsAsync(int eventId);

    Task<ImageData?> GetImageAsync(string folder, string fileName);
}

public class RaceDayApiClient : IRaceDayApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public RaceDayApiClient(HttpClient http)
    {
        _http = http;
    }

    // ---- Authentication ----
    public Task<UserDto> LoginAsync(string email, string password) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/login", new { email, password });

    public Task<UserDto> RegisterAsync(string fullName, string email, string password, string? phoneNumber, string role) =>
        SendAsync<UserDto>(HttpMethod.Post, "api/auth/register", new { fullName, email, password, phoneNumber, role });

    public Task LogoutAsync() => SendAsync(HttpMethod.Post, "api/auth/logout");

    // ---- Profile ----
    public Task<UserDto> GetProfileAsync(int userId) =>
        SendAsync<UserDto>(HttpMethod.Get, $"api/users/{userId}");

    public Task<UserDto> UpdateProfileAsync(int userId, string fullName, string? phoneNumber) =>
        SendAsync<UserDto>(HttpMethod.Put, $"api/users/{userId}", new { fullName, phoneNumber });

    public Task<UserDto> UploadProfilePictureAsync(int userId, IFormFile file) =>
        UploadAsync<UserDto>($"api/users/{userId}/profile-picture", file);

    // ---- Events ----
    public Task<List<EventDto>> GetEventsAsync(string? eventType, bool upcomingOnly)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            query.Add("eventType=" + Uri.EscapeDataString(eventType));
        }

        if (upcomingOnly)
        {
            query.Add("upcomingOnly=true");
        }

        var url = "api/events" + (query.Count > 0 ? "?" + string.Join("&", query) : string.Empty);
        return SendAsync<List<EventDto>>(HttpMethod.Get, url);
    }

    public Task<EventDetailDto> GetEventAsync(int eventId) =>
        SendAsync<EventDetailDto>(HttpMethod.Get, $"api/events/{eventId}");

    public Task<EventDto> CreateEventAsync(EventRequestDto request) =>
        SendAsync<EventDto>(HttpMethod.Post, "api/events", request);

    public Task<EventDto> UpdateEventAsync(int eventId, EventRequestDto request) =>
        SendAsync<EventDto>(HttpMethod.Put, $"api/events/{eventId}", request);

    public Task DeleteEventAsync(int eventId) =>
        SendAsync(HttpMethod.Delete, $"api/events/{eventId}");

    public Task<EventDto> UploadBannerAsync(int eventId, IFormFile file) =>
        UploadAsync<EventDto>($"api/events/{eventId}/banner", file);

    // ---- Categories ----
    public Task<List<CategoryDto>> GetCategoriesAsync(int eventId) =>
        SendAsync<List<CategoryDto>>(HttpMethod.Get, $"api/events/{eventId}/categories");

    public Task<CategoryDto> CreateCategoryAsync(int eventId, CategoryRequestDto request) =>
        SendAsync<CategoryDto>(HttpMethod.Post, $"api/events/{eventId}/categories", request);

    public Task<CategoryDto> UpdateCategoryAsync(int categoryId, CategoryRequestDto request) =>
        SendAsync<CategoryDto>(HttpMethod.Put, $"api/categories/{categoryId}", request);

    public Task DeleteCategoryAsync(int categoryId) =>
        SendAsync(HttpMethod.Delete, $"api/categories/{categoryId}");

    // ---- Enrolments ----
    public Task<EnrolmentDto> EnrolAsync(int eventId, int categoryId) =>
        SendAsync<EnrolmentDto>(HttpMethod.Post, $"api/events/{eventId}/enrolments", new { categoryId });

    public Task<List<EnrolmentDto>> GetMyEnrolmentsAsync() =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, "api/enrolments/my");

    public Task<List<EnrolmentDto>> GetEventEnrolmentsAsync(int eventId) =>
        SendAsync<List<EnrolmentDto>>(HttpMethod.Get, $"api/events/{eventId}/enrolments");

    public Task CancelEnrolmentAsync(int enrolmentId) =>
        SendAsync(HttpMethod.Delete, $"api/enrolments/{enrolmentId}");

    // ---- Results ----
    public Task<ResultDto> CaptureResultAsync(CaptureResultRequestDto request) =>
        SendAsync<ResultDto>(HttpMethod.Post, "api/results", request);

    public Task<List<ResultDto>> GetUserResultsAsync(int userId) =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/users/{userId}/results");

    public Task<List<ResultDto>> GetEventResultsAsync(int eventId) =>
        SendAsync<List<ResultDto>>(HttpMethod.Get, $"api/events/{eventId}/results");

    // ---- Images (always fetched through the API, never from Azure directly) ----
    public async Task<ImageData?> GetImageAsync(string folder, string fileName)
    {
        try
        {
            var url = $"api/images/{Uri.EscapeDataString(folder)}/{Uri.EscapeDataString(fileName)}";
            using var response = await SendRawAsync(HttpMethod.Get, url, null, null);
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return new ImageData(bytes, contentType);
        }
        catch (ApiException ex) when (ex.StatusCode == StatusCodes.Status404NotFound)
        {
            return null;
        }
    }

    // ---- Plumbing ----
    private async Task<T> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        using var response = await SendRawAsync(method, url, body, null);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result ?? throw new ApiException(StatusCodes.Status502BadGateway, "The service returned an empty response.");
    }

    private async Task SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var response = await SendRawAsync(method, url, body, null);
    }

    private async Task<T> UploadAsync<T>(string url, IFormFile file)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType);
        content.Add(fileContent, "file", file.FileName);

        using var response = await SendRawAsync(HttpMethod.Post, url, null, content);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        return result ?? throw new ApiException(StatusCodes.Status502BadGateway, "The service returned an empty response.");
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url);
        if (content is not null)
        {
            request.Content = content;
        }
        else if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (HttpRequestException)
        {
            throw new ApiUnavailableException("The RaceDay service is not reachable right now.");
        }
        catch (TaskCanceledException)
        {
            throw new ApiUnavailableException("The RaceDay service took too long to respond.");
        }

        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            var message = await ReadErrorAsync(response);
            response.Dispose();
            throw new ApiException(status, message);
        }

        return response;
    }

    /// <summary>Turns the API's error JSON into one readable sentence.</summary>
    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                    {
                        var messages = new List<string>();
                        foreach (var property in errors.EnumerateObject())
                        {
                            if (property.Value.ValueKind != JsonValueKind.Array)
                            {
                                continue;
                            }

                            foreach (var item in property.Value.EnumerateArray())
                            {
                                var message = item.GetString();
                                if (!string.IsNullOrWhiteSpace(message))
                                {
                                    messages.Add(message);
                                }
                            }
                        }

                        if (messages.Count > 0)
                        {
                            return string.Join(" ", messages);
                        }
                    }

                    if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                    {
                        var detailText = detail.GetString();
                        if (!string.IsNullOrWhiteSpace(detailText))
                        {
                            return detailText;
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // fall through to the generic message below
        }

        return (int)response.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => "Please log in to continue.",
            StatusCodes.Status403Forbidden => "You do not have permission to do that.",
            StatusCodes.Status404NotFound => "We could not find what you were looking for.",
            _ => "Something went wrong. Please try again."
        };
    }
}
