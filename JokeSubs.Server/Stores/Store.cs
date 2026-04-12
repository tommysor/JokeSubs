namespace JokeSubs.Server.Stores;

public sealed record StoreGroup(string Name);

public sealed record Store
{
	public required string Id { get; init; }

	public required string Name { get; init; }

	public IReadOnlyList<StoreGroup> Groups { get; init; } = [];
}