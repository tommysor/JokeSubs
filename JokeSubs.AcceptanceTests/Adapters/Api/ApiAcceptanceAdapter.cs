using System.Net.Http.Json;
using System.Text.Json;
using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Adapters.Api;

/// <summary>
/// Acceptance adapter that interacts with the application via HTTP API.
/// Uses HttpClient to call the /api/stores endpoints.
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

    public async Task<List<StoreItem>> GetStoresAsync()
    {
        var response = await _client.GetAsync("/api/stores");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var stores = JsonSerializer.Deserialize<List<StoreItem>>(content, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize stores from API response");

        return stores;
    }

    public async Task<CreateStoreResult> CreateStoreAsync(string id, string name)
    {
        var payload = new { id, name };
        var response = await _client.PostAsJsonAsync("/api/stores", payload);

        // 201 Created: successful creation
        if (response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            var content = await response.Content.ReadAsStringAsync();
            var store = JsonSerializer.Deserialize<StoreItem>(content, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize created store");
            return new CreateStoreResult
            {
                Store = store,
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
            return new CreateStoreResult
            {
                Store = null,
                ValidationError = error,
                Success = false
            };
        }

        // Any other response code is a hard failure
        throw new HttpRequestException(
            $"Unexpected response from Create Store endpoint: {response.StatusCode}. " +
            $"Body: {await response.Content.ReadAsStringAsync()}");
    }

    public async Task<int> GetStoreCountAsync()
    {
        var stores = await GetStoresAsync();
        return stores.Count;
    }

    public ValueTask DisposeAsync()
    {
        // HttpClient is managed by the fixture, no per-adapter cleanup needed
        return ValueTask.CompletedTask;
    }
}
