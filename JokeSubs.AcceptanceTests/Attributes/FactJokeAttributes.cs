using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Attributes;

/// <summary>
/// Helper class for using adapter-aware test attributes.
/// Usage:
///   [Theory]
///   [AllAdaptersData]
///   public async Task MyTest(AdapterKind adapter) { ... }
/// </summary>
public static class AdapterTestHelper
{
    /// <summary>
    /// Gets the theory data for "all adapters" tests (API and UI).
    /// </summary>
    public static AllAdaptersData AllAdapters => new();

    /// <summary>
    /// Gets the theory data for API-only tests.
    /// </summary>
    public static ApiOnlyAdapterData ApiOnly => new();

    /// <summary>
    /// Gets the theory data for UI-only tests.
    /// </summary>
    public static UiOnlyAdapterData UiOnly => new();
}

