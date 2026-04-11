namespace JokeSubs.AcceptanceTests.Infrastructure;

/// <summary>
/// Represents a store record as returned by the API.
/// </summary>
public record StoreItem(string Id, string Name);

/// <summary>
/// Represents validation errors returned by the API during store creation.
/// </summary>
public record ValidationErrorResult(
    string? Title,
    Dictionary<string, string[]> Errors
);

/// <summary>
/// Result of a store creation attempt.
/// </summary>
public record CreateStoreResult
{
    public required StoreItem? Store { get; init; }
    public required ValidationErrorResult? ValidationError { get; init; }
    public required bool Success { get; init; }
}

/// <summary>
/// Represents the contract for interacting with the SUT (System Under Test) via different transports.
/// Implementations include HTTP API and Playwright UI adapters.
/// </summary>
public interface IAcceptanceAdapter : IAsyncDisposable
{
    /// <summary>
    /// Loads the current list of stores.
    /// </summary>
    Task<List<StoreItem>> GetStoresAsync();

    /// <summary>
    /// Creates a new store with the given ID and name.
    /// Returns success/validation error information.
    /// </summary>
    Task<CreateStoreResult> CreateStoreAsync(string id, string name);

    /// <summary>
    /// Gets the count of active stores currently displayed.
    /// </summary>
    Task<int> GetStoreCountAsync();

}
