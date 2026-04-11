using Aspire.Hosting;
using Aspire.Hosting.Testing;
using JokeSubs.AcceptanceTests.Dsl;
using Xunit;

[assembly: AssemblyFixture(typeof(JokeSubs.AcceptanceTests.Infrastructure.AspireAssemblyFixture))]

namespace JokeSubs.AcceptanceTests.Infrastructure;

/// <summary>
/// Assembly-scoped fixture that starts the Aspire AppHost once and keeps it running for all tests.
/// The fixture manages the lifecycle: startup before any test, shutdown after all tests complete.
/// </summary>
public sealed class AspireAssemblyFixture : IAsyncLifetime
{
    private DistributedApplication? _app;
    public HttpClient ApiClient { get; private set; } = null!;
    public Uri UiBaseUri { get; private set; } = null!;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        // Create the testing builder. This will load and start the AppHost in a background process.
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.JokeSubs_AppHost>(
            args: [],
            configureBuilder: (appOptions, hostSettings) =>
            {
                // Disable the dashboard to reduce resource overhead in tests.
                appOptions.DisableDashboard = true;
            });

        _app = await appHost.BuildAsync();
        await _app.StartAsync();

        // Wait for resources to be healthy before creating clients.
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("server", cts.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("webfrontend", cts.Token);

        ApiClient = _app.CreateHttpClient("server");
        ApiClient.Timeout = TimeSpan.FromSeconds(30);

        using var frontendClient = _app.CreateHttpClient("webfrontend");
        UiBaseUri = frontendClient.BaseAddress
            ?? throw new InvalidOperationException("Unable to resolve webfrontend base address from Aspire test host.");
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_app != null)
        {
            await _app.DisposeAsync();
        }

        ApiClient?.Dispose();
    }

    public async Task<LocationScenarioDsl> GetLocationScenarioDsl(AdapterKind kind)
    {
        IAcceptanceAdapter adapter = kind switch
        {
            AdapterKind.Api => new Adapters.Api.ApiAcceptanceAdapter(this),
            AdapterKind.Ui => await Adapters.Ui.PlaywrightAcceptanceAdapter.CreateAsync(this),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Only a single adapter kind is supported per test execution.")
        };

        return new LocationScenarioDsl(adapter);
    }
}
