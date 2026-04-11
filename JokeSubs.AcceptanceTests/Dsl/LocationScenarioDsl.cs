using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Dsl;

/// <summary>
/// DSL context for location acceptance scenarios.
/// Provides Given/When/Then methods to setup, act on, and verify location operations.
/// </summary>
public class LocationScenarioDsl : IAsyncDisposable
{
    private readonly IAcceptanceAdapter _adapter;
    private List<LocationItem>? _currentLocations;
    private CreateLocationResult? _lastCreateResult;

    public LocationScenarioDsl(IAcceptanceAdapter adapter)
    {
        _adapter = adapter;
    }

    // ==================== GIVEN ====================

    public async Task GivenNoLocationsExistAsync()
    {
        // Keep a baseline snapshot for scenarios that need to compare before/after.
        // We do not enforce emptiness because Cosmos data may persist across runs.
        _currentLocations = await _adapter.GetLocationsAsync();
    }

    public async Task GivenLocationsExistAsync(params (string id, string name)[] locations)
    {
        foreach (var (id, name) in locations)
        {
            await WhenCreateLocationAsync(id, name);
            ThenLocationCreationSucceededAsync();
        }
    }

    // ==================== WHEN ====================

    public async Task WhenLoadingLocationsAsync()
    {
        _currentLocations = await _adapter.GetLocationsAsync();
    }

    public async Task WhenCreateLocationAsync(string id, string name)
    {
        _lastCreateResult = await _adapter.CreateLocationAsync(id, name);
    }

    // ==================== THEN ====================

    public async Task ThenLocationCountIsAsync(int expectedCount)
    {
        var actualCount = await _adapter.GetLocationCountAsync();
        if (actualCount != expectedCount)
        {
            throw new AssertionException(
                $"Expected {expectedCount} locations but found {actualCount}");
        }
    }

    public Task<int> GetLocationCountAsync()
    {
        return _adapter.GetLocationCountAsync();
    }

    public void ThenLocationCreationSucceededAsync()
    {
        if (_lastCreateResult?.Success != true)
        {
            var errorMsg = _lastCreateResult?.ValidationError?.Title
                ?? "Unknown error";
            throw new AssertionException(
                $"Location creation failed: {errorMsg}");
        }
    }

    public void ThenLocationCreationFailedAsync()
    {
        if (_lastCreateResult?.Success == true)
        {
            throw new AssertionException(
                "Location creation succeeded when it was expected to fail");
        }
    }

    public void ThenLocationExistsInListAsync(string id, string name)
    {
        var found = _currentLocations?.FirstOrDefault(l =>
            l.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
            l.Name == name);

        if (found == null)
        {
            throw new AssertionException(
                $"Expected location {id} ({name}) to exist in the list but it was not found");
        }
    }

    public void ThenLocationDoesNotExistInListAsync(string id)
    {
        var found = _currentLocations?.FirstOrDefault(l =>
            l.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (found != null)
        {
            throw new AssertionException(
                $"Expected location {id} to not exist but it was found");
        }
    }

    public void ThenValidationErrorExistsForFieldAsync(string fieldName, string expectedMessage)
    {
        if (_lastCreateResult?.ValidationError?.Errors == null)
        {
            throw new AssertionException(
                $"Expected validation error for field '{fieldName}' but no validation errors were returned");
        }

        var fieldKey = char.ToUpper(fieldName[0]) + fieldName.Substring(1);
        if (!_lastCreateResult.ValidationError.Errors.TryGetValue(fieldKey, out var messages))
        {
            throw new AssertionException(
                $"Expected validation error for field '{fieldName}' but no errors were found for that field");
        }

        var message = messages.FirstOrDefault();
        if (message != expectedMessage)
        {
            throw new AssertionException(
                $"Expected validation error '{expectedMessage}' but got '{message}'");
        }
    }

    public async Task ThenRefreshLocationsAsync()
    {
        await WhenLoadingLocationsAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _adapter.DisposeAsync();
    }
}

/// <summary>
/// Custom exception for DSL assertion failures that provides clear failure context.
/// </summary>
public class AssertionException : Exception
{
    public AssertionException(string message) : base(message) { }
}
