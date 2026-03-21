namespace JokeSubs.Server.Locations;

public sealed class InMemoryLocationStore : ILocationStore
{
    private readonly List<Location> _locations = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<Location> GetAll()
    {
        lock (_lock)
        {
            return _locations
                .OrderBy(location => location.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(location => location.Id, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public bool Exists(string id)
    {
        lock (_lock)
        {
            return _locations.Any(location => string.Equals(location.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public Location Add(CreateLocationRequest request)
    {
        var location = new Location(request.Id.Trim(), request.Name.Trim());

        lock (_lock)
        {
            _locations.Add(location);
        }

        return location;
    }
}