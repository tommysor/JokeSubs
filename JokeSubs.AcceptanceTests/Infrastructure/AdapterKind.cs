namespace JokeSubs.AcceptanceTests.Infrastructure;

/// <summary>
/// Represents which adapter(s) a test should run against.
/// </summary>
[Flags]
public enum AdapterKind
{
    /// <summary>
    /// Run against the HTTP API adapter.
    /// </summary>
    Api = 1,

    /// <summary>
    /// Run against the Playwright UI adapter.
    /// </summary>
    Ui = 2,

    /// <summary>
    /// Run against all adapters (both API and UI).
    /// </summary>
    All = Api | Ui
}
