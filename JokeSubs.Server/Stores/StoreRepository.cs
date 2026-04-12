using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace JokeSubs.Server.Stores;

public sealed class StoreRepository : IStoreRepository
{
    private readonly Container _container;

    public StoreRepository(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration["STORES_DATABASENAME"];
        var containerName = configuration["STORES_CONTAINERNAME"];

        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException(
                "Cosmos database and container names were not provided by Aspire. " +
                "Ensure the server references the AppHost Cosmos container resource.");
        }

        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<Store?> GetByIdAsync(string id)
    {
        var normalizedId = id.Trim().ToLowerInvariant();

        using var feed = _container.GetItemLinqQueryable<Store>()
            .Where(store => store.Id.ToLower() == normalizedId)
            .ToFeedIterator();

        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync();
            var store = page.Resource.FirstOrDefault();

            if (store is not null)
            {
                return store;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<Store>> GetAllAsync()
    {
        var results = new List<Store>();

        using var feed = _container.GetItemLinqQueryable<Store>().ToFeedIterator();
        while (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync();
            results.AddRange(page);
        }

        return results
            .OrderBy(l => l.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(l => l.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<bool> ExistsAsync(string id)
    {
        var normalizedId = id.Trim().ToLowerInvariant();

        using var feed = _container.GetItemLinqQueryable<Store>()
            .Where(s => s.Id.ToLower() == normalizedId)
            .Select(s => 1)
            .ToFeedIterator();
        if (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync();
            return page.Resource.FirstOrDefault() > 0;
        }

        return false;
    }

    public async Task<Store> AddAsync(CreateStoreRequest request)
    {
        var store = new Store
        {
            Id = request.Id.Trim(),
            Name = request.Name.Trim(),
            Groups = []
        };

        await _container.CreateItemAsync(store, new PartitionKey(store.Id));
        return store;
    }

    public async Task<Store?> AddGroupAsync(string storeId, AddStoreGroupRequest request)
    {
        var store = await GetByIdAsync(storeId);
        if (store is null)
        {
            return null;
        }

        var updatedStore = store with
        {
            Groups = [.. store.Groups, new StoreGroup(request.Name.Trim())]
        };

        await _container.ReplaceItemAsync(updatedStore, updatedStore.Id, new PartitionKey(updatedStore.Id));
        return updatedStore;
    }
}
