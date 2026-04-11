using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Dsl;

/// <summary>
/// DSL context for store acceptance scenarios.
/// Provides Given/When/Then methods to setup, act on, and verify store operations.
/// </summary>
public class StoreScenarioDsl : IAsyncDisposable
{
    private readonly IAcceptanceAdapter _adapter;
    private List<StoreItem>? _currentStores;
    private CreateStoreResult? _lastCreateResult;

    public StoreScenarioDsl(IAcceptanceAdapter adapter)
    {
        _adapter = adapter;
    }

    // ==================== GIVEN ====================

    public async Task GivenStoresExistAsync(params (string id, string name)[] stores)
    {
        foreach (var (id, name) in stores)
        {
            await WhenCreateStoreAsync(id, name);
            ThenStoreCreationSucceededAsync();
        }
    }

    // ==================== WHEN ====================

    public async Task WhenLoadingStoresAsync()
    {
        _currentStores = await _adapter.GetStoresAsync();
    }

    public async Task WhenCreateStoreAsync(string id, string name)
    {
        _lastCreateResult = await _adapter.CreateStoreAsync(id, name);
    }

    // ==================== THEN ====================

    public Task<int> GetStoreCountAsync()
    {
        return _adapter.GetStoreCountAsync();
    }

    public void ThenStoreCreationSucceededAsync()
    {
        if (_lastCreateResult?.Success != true)
        {
            var errorMsg = _lastCreateResult?.ValidationError?.Title
                ?? "Unknown error";
            throw new AssertionException(
                $"Store creation failed: {errorMsg}");
        }
    }

    public void ThenStoreCreationFailedAsync()
    {
        if (_lastCreateResult?.Success == true)
        {
            throw new AssertionException(
                "Store creation succeeded when it was expected to fail");
        }
    }

    public void ThenStoreExistsInListAsync(string id, string name)
    {
        var found = _currentStores?.FirstOrDefault(l =>
            l.Id.Equals(id, StringComparison.OrdinalIgnoreCase) &&
            l.Name == name);

        if (found == null)
        {
            throw new AssertionException(
                $"Expected store {id} ({name}) to exist in the list but it was not found");
        }
    }

    public void ThenStoreDoesNotExistInListAsync(string id)
    {
        var found = _currentStores?.FirstOrDefault(l =>
            l.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        if (found != null)
        {
            throw new AssertionException(
                $"Expected store {id} to not exist but it was found");
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

    public async Task ThenRefreshStoresAsync()
    {
        await WhenLoadingStoresAsync();
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
