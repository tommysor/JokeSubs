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

    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task AddsGroupToStore(AdapterKind adapterKind)
    {
        var uniqueId = $"group-store-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.GivenStoresExistAsync((uniqueId, "Group Store"));

        await dsl.WhenAddingGroupToStoreAsync(uniqueId, "Monthly Members");

        dsl.ThenGroupAdditionSucceededAsync();
        dsl.ThenStoreContainsGroupAsync("Monthly Members");
    }

    public static IEnumerable<object?[]> RejectsGroupAdditionWhenNameIsBlankData
        => AddAllAdaptersAsFirstArg(
        [
            [""],
            ["   "]
        ]);

    [Theory]
    [MemberData(nameof(RejectsGroupAdditionWhenNameIsBlankData))]
    public async Task RejectsGroupAdditionWhenNameIsBlank(AdapterKind adapterKind, string groupName)
    {
        var uniqueId = $"group-blank-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.GivenStoresExistAsync((uniqueId, "Group Store"));

        await dsl.WhenAddingGroupToStoreAsync(uniqueId, groupName);

        dsl.ThenGroupAdditionFailedAsync();
        dsl.ThenGroupValidationErrorExistsForFieldAsync("name", "Name is required.");
    }

    // ApiOnly because the UI does not allow submitting a null group name, but the API can receive it.
    [Theory]
    [MemberData(nameof(ApiOnlyData))]
    public async Task RejectsGroupAdditionWhenNameIsNull(AdapterKind adapterKind)
    {
        var uniqueId = $"group-null-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.GivenStoresExistAsync((uniqueId, "Group Store"));

        await dsl.WhenAddingGroupToStoreAsync(uniqueId, null!);

        dsl.ThenGroupAdditionFailedAsync();
        dsl.ThenGroupValidationErrorExistsForFieldAsync("name", "Name is required.");
    }

    [Theory]
    [MemberData(nameof(AllAdaptersData))]
    public async Task RejectsGroupAdditionWhenNameIsDuplicate(AdapterKind adapterKind)
    {
        var uniqueId = $"group-dup-{Guid.NewGuid():N}";
        await using var dsl = await _fixture.GetStoreScenarioDsl(adapterKind);

        await dsl.GivenStoresExistAsync((uniqueId, "Group Store"));

        await dsl.WhenAddingGroupToStoreAsync(uniqueId, "Members");
        dsl.ThenGroupAdditionSucceededAsync();

        await dsl.WhenAddingGroupToStoreAsync(uniqueId, "Members");

        dsl.ThenGroupAdditionFailedAsync();
        dsl.ThenGroupValidationErrorExistsForFieldAsync("name", "Group name must be unique within the store.");
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

    // ApiOnly because UI doesn't allow submitting null name, but API can receive it.
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
