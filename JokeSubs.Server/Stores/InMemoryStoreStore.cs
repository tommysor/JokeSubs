namespace JokeSubs.Server.Stores;

public sealed class InMemoryStoreStore : IStoreStore
{
    private readonly List<Store> _stores = [];
    private readonly Lock _lock = new();

    public Task<IReadOnlyList<Store>> GetAllAsync()
    {
        lock (_lock)
        {
            IReadOnlyList<Store> result = _stores
                .OrderBy(store => store.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(store => store.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return Task.FromResult(result);
        }
    }

    public Task<bool> ExistsAsync(string id)
    {
        lock (_lock)
        {
            bool result = _stores.Any(store =>
                string.Equals(store.Id, id, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(result);
        }
    }

    public Task<Store> AddAsync(CreateStoreRequest request)
    {
        var store = new Store(request.Id.Trim(), request.Name.Trim());

        lock (_lock)
        {
            _stores.Add(store);
        }

        return Task.FromResult(store);
    }
}