namespace JokeSubs.Server.Locations;

public interface ILocationStore
{
    IReadOnlyList<Location> GetAll();

    bool Exists(string id);

    Location Add(CreateLocationRequest request);
}