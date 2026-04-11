namespace JokeSubs.AcceptanceTests.Infrastructure;

/// <summary>
/// Represents a location record as returned by the API.
/// </summary>
public record LocationItem(string Id, string Name);

/// <summary>
/// Represents validation errors returned by the API during location creation.
/// </summary>
public record ValidationErrorResult(
    string? Title,
    Dictionary<string, string[]> Errors
);

/// <summary>
/// Result of a location creation attempt.
/// </summary>
public record CreateLocationResult
{
    public required LocationItem? Location { get; init; }
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
    /// Gets the kind of adapter (e.g., API or UI).
    /// </summary>
    AdapterKind Kind { get; }

    /// <summary>
    /// Loads the current list of locations.
    /// </summary>
    Task<List<LocationItem>> GetLocationsAsync();

    /// <summary>
    /// Creates a new location with the given ID and name.
    /// Returns success/validation error information.
    /// </summary>
    Task<CreateLocationResult> CreateLocationAsync(string id, string name);

    /// <summary>
    /// Gets the count of active locations currently displayed.
    /// </summary>
    Task<int> GetLocationCountAsync();

}
