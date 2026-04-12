# JokeSubs Acceptance Tests

This project contains end-to-end acceptance tests for the JokeSubs application, using xUnit v3 with a layered architecture for maintainability and flexibility.

## Architecture

The acceptance test suite is structured in three layers:

### Layer 1: Adapters
Adapters translate between the test DSL and actual system interactions:
- **ApiAcceptanceAdapter**: Interacts with the system via HTTP API using `HttpClient`
- **PlaywrightAcceptanceAdapter**: Interacts with the system via the web UI using Playwright

### Layer 2: DSL (Domain-Specific Language)
The `StoreScenarioDsl` class provides fluent Given/When/Then methods for test scenarios:
- `Given` methods set up test preconditions
- `When` methods perform actions
- `Then` methods verify outcomes

All DSL operations are adapter-agnostic and work with any adapter.

### Layer 3: Specs
Test specifications in `/Specs/Stores/StoreAcceptanceSpecs.cs` define acceptance scenarios using the DSL. Each test uses `[Theory]` with `[MemberData]` to automatically run against selected adapters.

## Acceptance Test Principles

Acceptance tests describe product features, not transport-specific implementations.

- The API and UI are different ways to use the same feature.
- A feature scenario should usually be written once and run against all adapters with `AllAdaptersData`.
- Use adapter-specific data such as `ApiOnlyData` or `UiOnlyData` only when the behavior is truly transport-specific. Document the reason with a comment.
- Name DSL operations in user terms, such as `WhenOpeningStoreAsync`, rather than transport terms.
- Keep transport details inside adapters. Specs and DSL should describe intent, not HTTP or DOM mechanics.

For UI adapters:
- Prefer selectors based on visible text or user-observable labels.
- Scope selectors to the relevant region or row before clicking.
- Avoid coupling tests to hidden attributes when a user-visible locator is available.
If a user-visible locator is not available, check why. This is likely indication that UI is not clear to user.

## Assembly Fixture

The `AspireAssemblyFixture` starts the Aspire AppHost once at the beginning of the test assembly and keeps it running for all tests. This ensures:
- Fast test execution (no repeated app startup/shutdown)
- Realistic integration testing against real services (Cosmos DB, etc.)
- Stable endpoints for adapters to target

## Running Tests

### All Tests
```bash
dotnet test JokeSubs.AcceptanceTests
```

### List Available Tests
```bash
dotnet test JokeSubs.AcceptanceTests --list-tests
```

### Run by Name Filter
```bash
dotnet test JokeSubs.AcceptanceTests --filter "CreateStoreSuccessfully"
```

### Run API Adapter Only
```bash
dotnet test JokeSubs.AcceptanceTests --filter "DisplayName~adapterKind: Api"
```

### Run UI Adapter Only
```bash
dotnet test JokeSubs.AcceptanceTests --filter "DisplayName~adapterKind: Ui"
```

### Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## Browser Setup (Playwright)

When running inside this repository's dev container, post-create setup automatically installs the Playwright Chromium browser used by UI adapter tests.

Outside the dev container, the first UI test run will download browsers. This is a one-time operation:

```bash
dotnet test JokeSubs.AcceptanceTests
# On first run, browsers are downloaded automatically
```

To manually install/verify browsers:
```bash
# Use Playwright CLI to install browsers
pwsh bin/Debug/net10.0/playwright install
```

Or use the npm version:
```bash
npx playwright install
```

## Writing New Tests

### Add a New Spec

Default to one feature scenario that runs against all adapters:

```csharp
[Theory]
[MemberData(nameof(AllAdaptersData))]
public async Task OpensStore(AdapterKind adapterKind)
{
    var uniqueId = $"open-store-{Guid.NewGuid():N}";
    await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

    await dsl.GivenStoresExistAsync((uniqueId, "Open Store"));

    await dsl.WhenOpeningStoreAsync(uniqueId);

    dsl.ThenStoreDetailsMatchAsync(uniqueId, "Open Store");
}
```

Use `ApiOnlyData` or `UiOnlyData` only if the scenario is genuinely specific to one transport.

### Add DSL Methods

When adding a new DSL operation:

- Name it after the user-visible feature or action.
- Do not create separate DSL methods for API and UI versions of the same behavior.
- Add one adapter contract operation for the feature intent, and let each adapter implement it in its own transport-specific way.

Example:

```csharp
public async Task WhenOpeningStoreAsync(string id)
{
    _selectedStore = await _adapter.OpenStoreAsync(id);
}
```

### Adapter-Specific Tests

Use adapter-specific data only when the behavior is actually transport-specific. For example, a validation rule exposed only through the API surface may use `ApiOnlyData`:
```csharp
public static IEnumerable<object[]> ApiOnlyData => 
    new[] { new object[] { AdapterKind.Api } };

[Theory]
[MemberData(nameof(ApiOnlyData))]
public async Task ApiOnlyBehavior(AdapterKind adapterKind)
{
    // This test will only run with the API adapter
}
```

## Troubleshooting

### Tests Hang on Startup
The fixture waits up to 30 seconds for the server to become healthy. If tests hang:
1. Check that the Aspire AppHost can start independently: `aspire run`
2. Verify Cosmos DB emulator is installed and working
3. Check firewall/port conflicts on ports 5364 (server) and 5173 (frontend)

### Browser Issues
If Playwright tests fail:
1. Ensure browsers are installed: `npx playwright install`
2. Check Chromium availability: `npx playwright install chromium`
3. For CI/headless environments, ensure xvfb-run or similar is available

### Port Conflicts
The fixture expects:
- Server API: `http://localhost:5364`
- Frontend: `http://localhost:5173`

If these ports are in use, tests will fail. Kill conflicting processes or adjust port configuration in `AspireAssemblyFixture`.

## Next Steps

### Add More Scenarios
Expand test coverage by adding error cases, edge cases, and integration paths:
- Validation error scenarios (duplicate ID, short ID, required fields)
- Concurrent operations
- Additional feature scenarios that run through all adapters

### Enhanced Diagnostics
Add screenshot and log capture for failed UI tests:
```csharp
catch (Exception)
{
    await adapter.TakeScreenshotAsync("failure.png");
    throw;
}
```

### Performance Monitoring
Track test execution times and add performance assertions:
```csharp
var timer = Stopwatch.StartNew();
await adapter.GetStoresAsync();
timer.Stop();
Assert.True(timer.ElapsedMilliseconds < 1000, "API response too slow");
```
