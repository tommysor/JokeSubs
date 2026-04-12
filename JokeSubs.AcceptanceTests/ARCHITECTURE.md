# Acceptance Test Architecture

## Overview

The JokeSubs acceptance test project (`JokeSubs.AcceptanceTests`) implements a three-layer, adapter-based testing architecture that enables Given/When/Then style acceptance tests to run against both HTTP API and web UI transports without test code duplication.

## Core Testing Principles

### Features Over Transports

Acceptance tests model product behavior. The API and UI are different transports for the same feature, not separate features.

This means:
- Write one scenario per feature.
- Run it across adapters using `AllAdaptersData` by default.
- Only split to `ApiOnlyData` or `UiOnlyData` when the behavior itself is transport-specific. This split needs to have a comment explaining why the test must be transport spesific.

### Intent-Based DSL and Adapter Contracts

DSL methods and adapter interfaces should express feature intent, not transport mechanics.

Prefer:
- `WhenOpeningStoreAsync`
- `OpenStoreAsync`

Avoid:
- transport-specific feature names when the user-facing behavior is the same across adapters.

Transport-specific details belong inside adapters.

### UI Selector Strategy

UI adapter selectors should follow this order of preference:

1. Visible user-facing text or labels
2. Scoped locators within the relevant UI region
3. Structural selectors when needed for precision
4. Hidden attributes only when no good user-facing locator exists. If no good user-facing locator exists, this is an indication that it is difficult for a user to use. Evaluate updating the UI to clarify for the user, and therefore providing a good user-facing locator.

```
┌─────────────────────────────────────────────────────────────┐
│                    Spec Tests Layer                         │
│            (Given/When/Then Scenario Definitions)           │
│                                                             │
│ - StoreAcceptanceSpecs.cs                               │
│ - Each scenario: 1 test × N adapters (API, UI)             │
│ - Uses [Theory] with [MemberData] for adapter selection    │
└─────────────────────────────────────────────────────────────┘
                              △
                              │
┌─────────────────────────────────────────────────────────────┐
│                      DSL Layer                              │
│              (Transport-Agnostic Operations)                │
│                                                             │
│ - StoreScenarioDsl.cs                                   │
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
- Exposes `StoreItem`, `ValidationErrorResult`, `CreateStoreResult` domain models
- Adapter kind enumeration: `AdapterKind.Api`, `AdapterKind.Ui`

**ApiAcceptanceAdapter** (`Adapters/Api/ApiAcceptanceAdapter.cs`)
- Uses HttpClient to call `/api/stores` endpoints
- JSON deserialization via `System.Text.Json`
- Maps HTTP status codes (201 created, 400 validation) to result types
- No browser or UI state—pure API interaction

**PlaywrightAcceptanceAdapter** (`Adapters/Ui/PlaywrightAcceptanceAdapter.cs`)
- Uses Playwright for browser automation
- Navigates to frontend UI
- Queries selectors: `input[name="id"]`, `input[name="name"]`, `.hero-stat-value`, `.store-name`, etc.
- Simulates user interactions (fill, click, wait)
- Parses UI state and error messages

### 3. DSL Layer

**StoreScenarioDsl** (`Dsl/StoreScenarioDsl.cs`)
- Fluent Given/When/Then operations
- Maintains scenario state (`_currentStores`, `_lastCreateResult`, `_selectedStore`)
- Methods use generic `IAcceptanceAdapter` interface, no adapter knowledge
- Examples:
   - `GivenStoresExistAsync(...)`
  - `WhenCreateStoreAsync(id, name)`
   - `WhenOpeningStoreAsync(id)`
  - `ThenStoreExistsInListAsync(id, name)`
   - `ThenStoreDetailsMatchAsync(id, name)`
  - `ThenValidationErrorExistsForFieldAsync(fieldName, message)`

### 4. Spec Layer

**StoreAcceptanceSpecs** (`Specs/Stores/StoreAcceptanceSpecs.cs`)
- Test class decorated with `[Collection("Aspire")]` to use assembly fixture
- Receives `AspireAssemblyFixture` via constructor injection
- Test methods use `[Theory]` with `[MemberData]` for adapter selection
- Default data source is `AllAdaptersData`; `ApiOnlyData` and `UiOnlyData` are reserved for transport-specific behavior
- Each test method requests one DSL from the fixture, then executes the same scenario through the selected adapter
- Current suite includes cross-adapter feature scenarios plus a small number of API-only validation cases

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
   ├─ Requests a DSL via fixture.GetStoreScenarioDsl(adapterKind)
   ├─ Fixture creates the matching adapter implementation
   ├─ Executes Given/When/Then operations
   ├─ Adapter translates to API or UI calls
   └─ DSL disposes the adapter after test

4. Scenarios execute identically on both adapters
   ├─ DSL ensures same preconditions / actions / assertions
   ├─ Adapters handle transport differences
   └─ Validates API and UI consistency
```

### Example: CreateStoreSuccessfully with API Adapter

```
Test Method Called
  └─ AdapterKind.Api passed as parameter
     │
     └─ fixture.GetStoreScenarioDsl(AdapterKind.Api)
        └─ Creates ApiAcceptanceAdapter with fixture.ApiClient
           │
           └─ StoreScenarioDsl wraps adapter
              │
              ├─ When: CreateStoreAsync("test-hub-1", "Test Hub 1")
              │     └─ adapter.CreateStoreAsync(id, name)
              │        └─ HTTP POST /api/stores {id, name}
              │           └─ Receives 201 Created + Store JSON
              │
              └─ Then: Assertions
                    ├─ ThenStoreCreationSucceededAsync()
                    │   └─ Checks _lastCreateResult.Success == true
                    ├─ WhenLoadingStoresAsync()
                    │   └─ adapter.GetStoresAsync() again
                    │      └─ HTTP GET /api/stores
                    └─ ThenStoreExistsInListAsync()
                        └─ Validates {id, name} in list
```

## Test Adapter Filtering

### Run All Tests
```bash
dotnet test JokeSubs.AcceptanceTests
# Runs the full acceptance suite across all configured adapters
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

1. Add test method to `StoreAcceptanceSpecs`
2. Use `[Theory]` and `[MemberData(nameof(AllAdaptersData))]` by default
3. Call DSL methods; adapters handle the transport details
4. No adapter-specific code in spec
5. Only use adapter-specific data when the behavior is transport-specific

### Adding a New DSL Operation

1. Add method to `StoreScenarioDsl` (e.g., `WhenRefreshStoresAsync()`)
2. Implement using adapter methods from `IAcceptanceAdapter`
3. All existing tests can now use the new DSL operation

### Adding a New Adapter

1. Implement `IAcceptanceAdapter`
2. Provide same operations as API and UI adapters
3. Update `AspireAssemblyFixture.GetStoreScenarioDsl()` to instantiate the new adapter
4. Add the new `AdapterKind` value
5. All existing specs run automatically on new adapter

## V1 Scope & Future Work

### Current (V1)

- **Three scenarios:** Load list, create store, persistence
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
