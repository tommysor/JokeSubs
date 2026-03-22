using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace JokeSubs.Server.Locations;

public sealed class CosmosLocationStore : ILocationStore
{
    private readonly Container _container;

    public CosmosLocationStore(CosmosClient cosmosClient, IConfiguration configuration)
    {
        var databaseName = configuration["LOCATIONS_DATABASENAME"];
        var containerName = configuration["LOCATIONS_CONTAINERNAME"];

        if (string.IsNullOrWhiteSpace(databaseName) || string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException(
                "Cosmos database and container names were not provided by Aspire. " +
                "Ensure the server references the AppHost Cosmos container resource.");
        }

        _container = cosmosClient.GetContainer(databaseName, containerName);
    }

    public async Task<IReadOnlyList<Location>> GetAllAsync()
    {
        var query = new QueryDefinition("SELECT c.id, c.name FROM c");
        var results = new List<Location>();

        using var feed = _container.GetItemQueryIterator<Location>(query);
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

        // Use LOWER() on the stored id to enforce case-insensitive uniqueness,
        // matching the behaviour of the in-memory store.
        var query = new QueryDefinition(
            "SELECT VALUE COUNT(1) FROM c WHERE LOWER(c.id) = @normalizedId")
            .WithParameter("@normalizedId", normalizedId);

        using var feed = _container.GetItemQueryIterator<int>(query);
        if (feed.HasMoreResults)
        {
            var page = await feed.ReadNextAsync();
            return page.Resource.FirstOrDefault() > 0;
        }

        return false;
    }

    public async Task<Location> AddAsync(CreateLocationRequest request)
    {
        var location = new Location(request.Id.Trim(), request.Name.Trim());
        await _container.CreateItemAsync(location, new PartitionKey(location.Id));
        return location;
    }
}
