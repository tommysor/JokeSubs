namespace JokeSubs.AcceptanceTests.Infrastructure;

/// <summary>
/// Factory for creating acceptance test adapters.
/// Supports creating API and UI adapters, and can enumerate all requested adapters for a test.
/// </summary>
public class AdapterFactory
{
    private readonly AspireAssemblyFixture _fixture;

    public AdapterFactory(AspireAssemblyFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Creates all adapters matching the given kind.
    /// </summary>
    public async Task<List<IAcceptanceAdapter>> CreateAdaptersAsync(AdapterKind kind)
    {
        var adapters = new List<IAcceptanceAdapter>();

        if (kind.HasFlag(AdapterKind.Api))
        {
            adapters.Add(new Adapters.Api.ApiAcceptanceAdapter(_fixture));
        }

        if (kind.HasFlag(AdapterKind.Ui))
        {
            adapters.Add(await Adapters.Ui.PlaywrightAcceptanceAdapter.CreateAsync(_fixture));
        }

        return adapters;
    }
}
