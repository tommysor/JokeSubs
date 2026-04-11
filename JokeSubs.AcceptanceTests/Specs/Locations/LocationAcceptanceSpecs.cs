using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Specs.Locations;

public class LocationAcceptanceSpecs
{
    private readonly AspireAssemblyFixture _fixture;

    public LocationAcceptanceSpecs(AspireAssemblyFixture fixture)
    {
        _fixture = fixture;
    }

    // Theory data helpers for adapter selection
    private static IEnumerable<object?[]> AddAllAdaptersAsFirstArg(IEnumerable<object?[]> data)
    {
        var adapterKinds = Enum.GetValues<AdapterKind>();
        foreach (var adapterKind in adapterKinds)
        {
            foreach (var item in data)
            {
                var newItem = new object?[item.Length + 1];
                newItem[0] = adapterKind;
                Array.Copy(item, 0, newItem, 1, item.Length);
                yield return newItem;
            }
        }
    }

    public static IEnumerable<object?[]> AllAdaptersData =>
        AddAllAdaptersAsFirstArg(
        [
            [],
        ]);

    public static IEnumerable<object[]> ApiOnlyData =>
        new[] { new object[] { AdapterKind.Api } };

    public static IEnumerable<object[]> UiOnlyData =>
        new[] { new object[] { AdapterKind.Ui } };

    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task LoadsLocationsOnInitialPageLoad(AdapterKind adapterKind)
    {
        await using var dsl = await _fixture.GetLocationScenarioDsl(adapterKind);

        // When: We load the locations
        await dsl.WhenLoadingLocationsAsync();

        // Then: The location count is available and non-negative
        var count = await dsl.GetLocationCountAsync();
        Assert.True(count >= 0);
    }

    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task CreatesLocationSuccessfully(AdapterKind adapterKind)
    {
        var uniqueId = $"test-hub-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetLocationScenarioDsl(adapterKind);

        // When: We create a new location
        await dsl.WhenCreateLocationAsync(uniqueId, "Test Hub 1");

        // Then: The creation succeeded
        dsl.ThenLocationCreationSucceededAsync();

        // And: The location now appears in the list
        await dsl.WhenLoadingLocationsAsync();
        dsl.ThenLocationExistsInListAsync(uniqueId, "Test Hub 1");
    }

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

    public static IEnumerable<object?[]> RejectsLocationCreationWhenNameIsBlankData
        => AddAllAdaptersAsFirstArg(
        [
            [""],
            ["   "]
        ]);

    [Theory]
    [MemberData(nameof(RejectsLocationCreationWhenNameIsBlankData))]
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
