namespace JokeSubs.Server.Locations;

public sealed class InMemoryLocationStore : ILocationStore
{
    private readonly List<Location> _locations = [];
    private readonly Lock _lock = new();

    public Task<IReadOnlyList<Location>> GetAllAsync()
    {
        lock (_lock)
        {
            IReadOnlyList<Location> result = _locations
                .OrderBy(location => location.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(location => location.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return Task.FromResult(result);
        }
    }

    public Task<bool> ExistsAsync(string id)
    {
        lock (_lock)
        {
            bool result = _locations.Any(location =>
                string.Equals(location.Id, id, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(result);
        }
    }

    public Task<Location> AddAsync(CreateLocationRequest request)
    {
        var location = new Location(request.Id.Trim(), request.Name.Trim());

        lock (_lock)
        {
            _locations.Add(location);
        }

        return Task.FromResult(location);
    }
}