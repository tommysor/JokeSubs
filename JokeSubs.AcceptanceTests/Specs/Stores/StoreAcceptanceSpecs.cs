using JokeSubs.AcceptanceTests.Infrastructure;

namespace JokeSubs.AcceptanceTests.Specs.Stores;

public class StoreAcceptanceSpecs
{
    private readonly AspireAssemblyFixture _fixture;

    public StoreAcceptanceSpecs(AspireAssemblyFixture fixture)
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
    public async Task LoadsStoresOnInitialPageLoad(AdapterKind adapterKind)
    {
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.WhenLoadingStoresAsync();

        var count = await dsl.GetStoreCountAsync();
        Assert.True(count >= 0);
    }

    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task CreatesStoreSuccessfully(AdapterKind adapterKind)
    {
        var uniqueId = $"test-hub-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.WhenCreateStoreAsync(uniqueId, "Test Hub 1");

        dsl.ThenStoreCreationSucceededAsync();

        await dsl.WhenLoadingStoresAsync();
        dsl.ThenStoreExistsInListAsync(uniqueId, "Test Hub 1");
    }

    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task PersistsStoresAcrossRequests(AdapterKind adapterKind)
    {
        var idA = $"loc-a-{Guid.NewGuid():N}";
        var idB = $"loc-b-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.GivenStoresExistAsync(
            (idA, "Store A"),
            (idB, "Store B")
        );

        await dsl.ThenRefreshStoresAsync();

        dsl.ThenStoreExistsInListAsync(idA, "Store A");
        dsl.ThenStoreExistsInListAsync(idB, "Store B");
    }

    public static IEnumerable<object?[]> RejectsStoreCreationWhenNameIsBlankData
        => AddAllAdaptersAsFirstArg(
        [
            [""],
            ["   "]
        ]);

    [Theory]
    [MemberData(nameof(RejectsStoreCreationWhenNameIsBlankData))]
    public async Task RejectsStoreCreationWhenNameIsBlank(AdapterKind adapterKind, string name)
    {
        var uniqueId = $"loc-name-required-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.WhenCreateStoreAsync(uniqueId, name);

        dsl.ThenStoreCreationFailedAsync();
        dsl.ThenValidationErrorExistsForFieldAsync("name", "Name is required.");
    }

    [Theory]
    [MemberData(nameof(ApiOnlyData))]
    public async Task RejectsStoreCreationWhenNameIsNull(AdapterKind adapterKind)
    {
        var uniqueId = $"loc-name-required-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.WhenCreateStoreAsync(uniqueId, null!);

        dsl.ThenStoreCreationFailedAsync();
        dsl.ThenValidationErrorExistsForFieldAsync("name", "Name is required.");
    }
}
