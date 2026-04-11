using Xunit;
using JokeSubs.AcceptanceTests.Attributes;
using JokeSubs.AcceptanceTests.Dsl;
using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Specs.Locations;

/// <summary>
/// Acceptance tests for location management features.
/// These tests specify end-to-end scenarios in Given/When/Then format,
/// executable against both UI and API adapters.
/// </summary>
public class LocationAcceptanceSpecs
{
    private readonly AspireAssemblyFixture _fixture;

    public LocationAcceptanceSpecs(AspireAssemblyFixture fixture)
    {
        _fixture = fixture;
    }

    // Theory data helpers for adapter selection
    public static IEnumerable<object[]> AllAdaptersData =>
        new[]
        {
            new object[] { AdapterKind.Api },
            new object[] { AdapterKind.Ui }
        };

    public static IEnumerable<object[]> ApiOnlyData =>
        new[] { new object[] { AdapterKind.Api } };

    public static IEnumerable<object[]> UiOnlyData =>
        new[] { new object[] { AdapterKind.Ui } };

    public static IEnumerable<object?[]> InvalidNameData =>
        new[]
        {
            new object?[] { AdapterKind.Api, "" },
            new object?[] { AdapterKind.Api, "   " },
            new object?[] { AdapterKind.Ui, "" },
            new object?[] { AdapterKind.Ui, "   " }
        };

    // ============================================================================
    // CORE HAPPY PATH TESTS (v1 scope)
    // ============================================================================

    /// <summary>
    /// Scenario: User loads the application and sees the locations list
    /// Given: No previous setup
    /// When: The locations page is loaded
    /// Then: The active locations count is displayed
    /// </summary>
    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task LoadsLocationsOnInitialPageLoad(AdapterKind adapterKind)
    {
        await using var dsl = await _fixture.GetLocationScenarioDsl(adapterKind);

        // Given: We're starting fresh
        await dsl.GivenNoLocationsExistAsync();

        // When: We load the locations
        await dsl.WhenLoadingLocationsAsync();

        // Then: The location count is available and non-negative
        var count = await dsl.GetLocationCountAsync();
        Assert.True(count >= 0);
    }

    /// <summary>
    /// Scenario: User creates a new location successfully
    /// Given: No locations exist
    /// When: User submits a valid location form
    /// Then: The location appears in the list immediately
    /// </summary>
    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task CreatesLocationSuccessfully(AdapterKind adapterKind)
    {
        var uniqueId = $"test-hub-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetLocationScenarioDsl(adapterKind);

        // Given: We're starting fresh
        await dsl.GivenNoLocationsExistAsync();

        // When: We create a new location
        await dsl.WhenCreateLocationAsync(uniqueId, "Test Hub 1");

        // Then: The creation succeeded
        dsl.ThenLocationCreationSucceededAsync();

        // And: The location now appears in the list
        await dsl.WhenLoadingLocationsAsync();
        dsl.ThenLocationExistsInListAsync(uniqueId, "Test Hub 1");
    }

    /// <summary>
    /// Scenario: User sees the initial location in the list (integration with Cosmos)
    /// Given: Locations created via the API before the current session
    /// When: The page loads
    /// Then: The previously created locations are visible
    /// </summary>
    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task PersistsLocationsAcrossRequests(AdapterKind adapterKind)
    {
        var idA = $"loc-a-{Guid.NewGuid():N}";
        var idB = $"loc-b-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetLocationScenarioDsl(adapterKind);

        // Given: We create multiple locations
        await dsl.GivenLocationsExistAsync(
            (idA, "Location A"),
            (idB, "Location B")
        );

        // When: We refresh the locations list
        await dsl.ThenRefreshLocationsAsync();

        // Then: Both locations are still visible
        dsl.ThenLocationExistsInListAsync(idA, "Location A");
        dsl.ThenLocationExistsInListAsync(idB, "Location B");
    }

    /// <summary>
    /// Scenario: User submits a location without a name
    /// Given: No specific setup beyond loading baseline state
    /// When: User submits a location with a blank name
    /// Then: Validation fails with a name error and no location is added
    /// </summary>
    [Theory]
    [MemberData(nameof(InvalidNameData))]
    public async Task RejectsLocationCreationWhenNameIsBlank(AdapterKind adapterKind, string name)
    {
        var uniqueId = $"loc-name-required-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetLocationScenarioDsl(adapterKind);

        await dsl.WhenCreateLocationAsync(uniqueId, name);

        dsl.ThenLocationCreationFailedAsync();
        dsl.ThenValidationErrorExistsForFieldAsync("name", "Name is required.");
    }

    [Theory]
    [MemberData(nameof(ApiOnlyData))]
    public async Task RejectsLocationCreationWhenNameIsNull(AdapterKind adapterKind)
    {
        var uniqueId = $"loc-name-required-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetLocationScenarioDsl(adapterKind);

        await dsl.WhenCreateLocationAsync(uniqueId, null!);

        dsl.ThenLocationCreationFailedAsync();
        dsl.ThenValidationErrorExistsForFieldAsync("name", "Name is required.");
    }
}
