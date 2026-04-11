namespace JokeSubs.AcceptanceTests.Infrastructure;

/// <summary>
/// Factory for creating acceptance test adapters.
/// Supports creating a single API or UI adapter for a test execution.
/// </summary>
public class AdapterFactory
{
    private readonly AspireAssemblyFixture _fixture;

    public AdapterFactory(AspireAssemblyFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Creates a single adapter for the given kind.
    /// </summary>
    public async Task<IAcceptanceAdapter> CreateAdapterAsync(AdapterKind kind)
    {
        return kind switch
        {
            AdapterKind.Api => new Adapters.Api.ApiAcceptanceAdapter(_fixture),
            AdapterKind.Ui => await Adapters.Ui.PlaywrightAcceptanceAdapter.CreateAsync(_fixture),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only a single adapter kind is supported per test execution.")
        };
    }
}
