# Acceptance Test Architecture

## Overview

The JokeSubs acceptance test project (`JokeSubs.AcceptanceTests`) implements a three-layer, adapter-based testing architecture that enables Given/When/Then style acceptance tests to run against both HTTP API and web UI transports without test code duplication.

```
┌─────────────────────────────────────────────────────────────┐
│                    Spec Tests Layer                         │
│            (Given/When/Then Scenario Definitions)           │
│                                                             │
│ - LocationAcceptanceSpecs.cs                               │
│ - Each scenario: 1 test × N adapters (API, UI)             │
│ - Uses [Theory] with [MemberData] for adapter selection    │
└─────────────────────────────────────────────────────────────┘
                              △
                              │
┌─────────────────────────────────────────────────────────────┐
│                      DSL Layer                              │
│              (Transport-Agnostic Operations)                │
│                                                             │
│ - LocationScenarioDsl.cs                                   │
│ - Fluent interface: Given/When/Then methods                │
│ - Uses IAcceptanceAdapter abstraction                      │
│ - No adapter-specific code                                 │
└─────────────────────────────────────────────────────────────┘
                              △
                              │ uses
┌─────────────────────────────────────────────────────────────┐
│                  Adapter Layer                              │
│          (Transport-Specific Implementations)              │
│                                                             │
│ ┌─────────────────────┐  ┌──────────────────────────────┐ │
│ │ ApiAcceptanceAdapter│  │ PlaywrightAcceptanceAdapter │ │
│ │                     │  │                              │ │
│ │ - HttpClient        │  │ - Playwright browser control │ │
│ │ - JSON serialization│  │ - DOM selector queries       │ │
│ │ - HTTP endpoints    │  │ - Form interaction          │ │
│ └─────────────────────┘  └──────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
                              △
                              │
┌─────────────────────────────────────────────────────────────┐
│                    Infrastructure                           │
│          (Aspire AppHost Lifecycle Management)             │
│                                                             │
│ - AspireAssemblyFixture: xUnit v3 assembly-scoped         │
│   DistributedApplication starts/stops once per test run   │
│ - Manages: Cosmos DB, Server API, Frontend UI             │
│ - Provides: HttpClient, UIBaseUri for adapters            │
└─────────────────────────────────────────────────────────────┘
```

## Key Components

### 1. Infrastructure Layer

**AspireAssemblyFixture** (`Infrastructure/AspireAssemblyFixture.cs`)
- Implements `IAsyncLifetime` for xUnit v3
- Uses `DistributedApplicationTestingBuilder` to start AppHost
- Runs once per test assembly, shared by all tests
- Exposes `HttpClient` and `UiBaseUri` for adapter use
- Includes health check polling (60 attempts, 500ms each)

### 2. Adapter Layer

**IAcceptanceAdapter** (`Infrastructure/IAcceptanceAdapter.cs`)
- Contract defining operations all adapters must support
- Exposes `LocationItem`, `ValidationErrorResult`, `CreateLocationResult` domain models
- Adapter kind enumeration: `AdapterKind.Api`, `AdapterKind.Ui`, `AdapterKind.All`

**ApiAcceptanceAdapter** (`Adapters/Api/ApiAcceptanceAdapter.cs`)
- Uses HttpClient to call `/api/locations` endpoints
- JSON deserialization via `System.Text.Json`
- Maps HTTP status codes (201 created, 400 validation) to result types
- No browser or UI state—pure API interaction

**PlaywrightAcceptanceAdapter** (`Adapters/Ui/PlaywrightAcceptanceAdapter.cs`)
- Uses Playwright for browser automation
- Navigates to frontend UI
- Queries selectors: `input[name="id"]`, `input[name="name"]`, `.hero-stat-value`, `.location-name`, etc.
- Simulates user interactions (fill, click, wait)
- Parses UI state and error messages

**AdapterFactory** (`Infrastructure/AdapterFactory.cs`)
- Factory for creating adapter instances
- Routes `AdapterKind` to correct adapter implementation
- Supports multiple adapters per test run for comparative verification

### 3. DSL Layer

**LocationScenarioDsl** (`Dsl/LocationScenarioDsl.cs`)
- Fluent Given/When/Then operations
- Maintains scenario state (`_currentLocations`, `_lastCreateResult`)
- Methods use generic `IAcceptanceAdapter` interface, no adapter knowledge
- Examples:
  - `GivenNoLocationsExistAsync()`
  - `WhenCreateLocationAsync(id, name)`
  - `ThenLocationExistsInListAsync(id, name)`
  - `ThenValidationErrorExistsForFieldAsync(fieldName, message)`

### 4. Spec Layer

**LocationAcceptanceSpecs** (`Specs/Locations/LocationAcceptanceSpecs.cs`)
- Test class decorated with `[Collection("Aspire")]` to use assembly fixture
- Receives `AspireAssemblyFixture` via constructor injection
- Test methods use `[Theory]` with `[MemberData]` for adapter selection
- Data sources: `AllAdaptersData`, `ApiOnlyData`, `UiOnlyData`
- Each test method creates adapters, wraps them in DSL, executes scenario
- Current v1 scope: 3 scenarios × 2 adapters = 6 tests

## Data Flow

### Test Execution

```
1. xUnit discovers tests from [MemberData] helper methods
   ├─ AllAdaptersData yields [AdapterKind.Api], [AdapterKind.Ui]
   ├─ Creates two test cases per scenario
   └─ Display names: "ScenarioName [Api]", "ScenarioName [Ui]"

2. Test runner injects AspireAssemblyFixture into test class
   ├─ Fixture startup: DistributedApplication starts once
   ├─ Fixture shared across all test methods in collection
   └─ Fixture cleanup after all tests complete

3. Test method executes with AdapterKind parameter
   ├─ Creates adapter(s) via AdapterFactory
   ├─ Wraps adapter(s) in LocationScenarioDsl
   ├─ Executes Given/When/Then operations
   ├─ Adapter translates to API or UI calls
   └─ Adapter disposes resources after test

4. Scenarios execute identically on both adapters
   ├─ DSL ensures same preconditions / actions / assertions
   ├─ Adapters handle transport differences
   └─ Validates API and UI consistency
```

### Example: CreateLocationSuccessfully with API Adapter

```
Test Method Called
  └─ AdapterKind.Api passed as parameter
     │
     └─ AdapterFactory.CreateAdaptersAsync(AdapterKind.Api)
        └─ Creates ApiAcceptanceAdapter with fixture.ApiClient
           │
           └─ LocationScenarioDsl wraps adapter
              │
              ├─ Given: GivenNoLocationsExistAsync()
              │     └─ adapter.GetLocationsAsync()
              │        └─ HTTP GET /api/locations
              │           └─ Parses JSON, asserts empty list
              │
              ├─ When: CreateLocationAsync("test-hub-1", "Test Hub 1")
              │     └─ adapter.CreateLocationAsync(id, name)
              │        └─ HTTP POST /api/locations {id, name}
              │           └─ Receives 201 Created + Location JSON
              │
              └─ Then: Assertions
                    ├─ ThenLocationCreationSucceededAsync()
                    │   └─ Checks _lastCreateResult.Success == true
                    ├─ WhenLoadingLocationsAsync()
                    │   └─ adapter.GetLocationsAsync() again
                    │      └─ HTTP GET /api/locations
                    └─ ThenLocationExistsInListAsync()
                        └─ Validates {id, name} in list
```

## Test Adapter Filtering

### Run All Tests
```bash
dotnet test JokeSubs.AcceptanceTests
# Runs: 6 tests (3 scenarios × 2 adapters)
```

### Run API Only
```bash
dotnet test JokeSubs.AcceptanceTests --filter "Api)"
# Runs: 3 tests (3 scenarios × API only)
# Useful for: Fast API-only validation, CI pre-checks
```

### Run UI Only
```bash
dotnet test JokeSubs.AcceptanceTests --filter "Ui)"
# Runs: 3 tests (3 scenarios × UI only)
# Useful for: UI-specific debugging, browser testing
```

## Extensibility

### Adding a New Scenario

1. Add test method to `LocationAcceptanceSpecs`
2. Use `[Theory]` and `[MemberData(nameof(AllAdaptersData))]` (or API/UI variants)
3. Call DSL methods; adapters handle the transport details
4. No adapter-specific code in spec

### Adding a New DSL Operation

1. Add method to `LocationScenarioDsl` (e.g., `WhenRefreshLocationsAsync()`)
2. Implement using adapter methods from `IAcceptanceAdapter`
3. All existing tests can now use the new DSL operation

### Adding a New Adapter

1. Implement `IAcceptanceAdapter`
2. Provide same operations as API and UI adapters
3. Update `AdapterFactory.CreateAdaptersAsync()` to instantiate new adapter
4. Update `IAcceptanceAdapter.Kind` with new `AdapterKind` flag value
5. All existing specs run automatically on new adapter

## V1 Scope & Future Work

### Current (V1)

- **Three scenarios:** Load list, create location, persistence
- **Two adapters:** API (HttpClient), UI (Playwright)
- **DSL:** Basic Given/When/Then for happy path
- **Fixture:** Assembly-scoped, real Cosmos DB emulator

### Future Enhancements

- **Error scenarios:** Validation failures, duplicate ID, required fields
- **Advanced DSL:** Async retry logic, UI waits, error recovery
- **Performance tests:** Response time assertions
- **Tracing/Logging:** Capture Aspire logs on test failure
- **Database reset:** Per-test cleanup for isolation
- **Concurrent operations:** Multi-user scenario simulation
- **Mobile/responsive:** Browser form-factor variations
- **Accessibility:** WCAG compliance checks via adapters

## Dependency Structure

```
JokeSubs.AcceptanceTests
├─ [project ref] JokeSubs.AppHost     (for DistributedApplicationTestingBuilder<T>)
├─ [project ref] JokeSubs.Server      (type definitions if needed)
├─ [nuget] Aspire.Hosting.Testing     (testing builder, distributed app lifecycle)
├─ [nuget] Microsoft.Playwright       (browser automation)
├─ [nuget] xunit.v3                   (test framework)
└─ [nuget] xunit.abstractions         (xUnit extensibility)
```

## Design Rationale

### Why Three Layers?

1. **Adapter Layer:** Isolates transport concerns (HTTP vs. browser)
2. **DSL Layer:** Expresses intent clearly (Given/When/Then)
3. **Spec Layer:** Remains focused on scenarios, not mechanics

Separation enables:
- Independent adapter development
- DSL reuse across adapters
- Spec clarity and expressiveness
- Easy refactoring without changing tests

### Why Assembly Fixture?

- **Speed:** Single AppHost startup (30-60s) vs. per-test (3-10min for full suite)
- **Realism:** Real services (Cosmos), real networking, real state
- **Stability:** Reduced process lifecycle churn, clearer error diagnosis
- **Simplicity:** Shared fixture = less boilerplate per test

### Why MemberData Instead of Custom Attribute?

- **xUnit v3 Alignment:** MemberData is first-class, custom FactAttribute is advanced
- **Clarity:** Data source is explicit and visible in test code
- **Maintainability:** No xUnit internals to override or patch
- **Discovery:** Works reliably with dotnet test runner

---

**Architecture Version:** 1.0
**Framework:** xUnit v3  
**Target Framework:** .NET 10.0  
**Last Updated:** 2026-03-29
