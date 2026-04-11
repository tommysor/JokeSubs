using System.Net.Http.Json;
using System.Text.Json;
using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Adapters.Api;

/// <summary>
/// Acceptance adapter that interacts with the application via HTTP API.
/// Uses HttpClient to call the /api/locations endpoints.
/// </summary>
public class ApiAcceptanceAdapter : IAcceptanceAdapter
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ApiAcceptanceAdapter(AspireAssemblyFixture fixture, HttpClient client)
    {
        _client = client;
    }

    public async Task<List<LocationItem>> GetLocationsAsync()
    {
        var response = await _client.GetAsync("/api/locations");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var locations = JsonSerializer.Deserialize<List<LocationItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize locations from API response");

        return locations;
    }

    public async Task<CreateLocationResult> CreateLocationAsync(string id, string name)
    {
        var payload = new { id, name };
        var response = await _client.PostAsJsonAsync("/api/locations", payload);

        // 201 Created: successful creation
        if (response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            var content = await response.Content.ReadAsStringAsync();
            var location = JsonSerializer.Deserialize<LocationItem>(content, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize created location");
            return new CreateLocationResult
            {
                Location = location,
                ValidationError = null,
                Success = true
            };
        }

        // 400 Bad Request: validation error
        if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            var error = JsonSerializer.Deserialize<ValidationErrorResult>(content, JsonOptions)
                ?? new ValidationErrorResult(null, new Dictionary<string, string[]>());
            return new CreateLocationResult
            {
                Location = null,
                ValidationError = error,
                Success = false
            };
        }

        // Any other response code is a hard failure
        throw new HttpRequestException(
            $"Unexpected response from Create Location endpoint: {response.StatusCode}. " +
            $"Body: {await response.Content.ReadAsStringAsync()}");
    }

    public async Task<int> GetLocationCountAsync()
    {
        var locations = await GetLocationsAsync();
        return locations.Count;
    }

    public ValueTask DisposeAsync()
    {
        // HttpClient is managed by the fixture, no per-adapter cleanup needed
        return ValueTask.CompletedTask;
    }
}
