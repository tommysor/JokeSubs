namespace JokeSubs.Server.Stores;

public interface IStoreStore
{
    Task<IReadOnlyList<Store>> GetAllAsync();

    Task<bool> ExistsAsync(string id);

    Task<Store> AddAsync(CreateStoreRequest request);
}