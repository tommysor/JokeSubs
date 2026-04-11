namespace JokeSubs.Server.Stores;

public interface IStoreRepository
{
    Task<IReadOnlyList<Store>> GetAllAsync();

    Task<bool> ExistsAsync(string id);

    Task<Store> AddAsync(CreateStoreRequest request);
}