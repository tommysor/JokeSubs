namespace JokeSubs.Server.Locations;

public interface ILocationStore
{
    Task<IReadOnlyList<Location>> GetAllAsync();

    Task<bool> ExistsAsync(string id);

    Task<Location> AddAsync(CreateLocationRequest request);
}