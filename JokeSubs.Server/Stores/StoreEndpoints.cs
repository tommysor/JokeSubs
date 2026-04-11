using Microsoft.AspNetCore.Http.HttpResults;

namespace JokeSubs.Server.Stores;

public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stores")
            .WithTags("Stores");

        group.MapGet("", async (IStoreStore storeStore) =>
            TypedResults.Ok(await storeStore.GetAllAsync()))
            .WithName("GetStores");

        group.MapPost("", async Task<Results<Created<Store>, ValidationProblem>> (CreateStoreRequest request, IStoreStore storeStore) =>
        {
            var errors = await ValidateRequest(request, storeStore);

            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var createdStore = await storeStore.AddAsync(request);
            return TypedResults.Created($"/api/stores/{Uri.EscapeDataString(createdStore.Id)}", createdStore);
        })
        .WithName("CreateStore");

        return app;
    }

    private static async Task<Dictionary<string, string[]>> ValidateRequest(CreateStoreRequest request, IStoreStore store)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var trimmedId = request.Id?.Trim() ?? string.Empty;
        var trimmedName = request.Name?.Trim() ?? string.Empty;

        if (trimmedId.Length == 0)
        {
            errors[nameof(request.Id)] = ["Id is required."];
        }
        else if (trimmedId.Length < 4)
        {
            errors[nameof(request.Id)] = ["Id must be at least 4 characters long."];
        }
        else if (await store.ExistsAsync(trimmedId))
        {
            errors[nameof(request.Id)] = ["Id must be unique."];
        }

        if (trimmedName.Length == 0)
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }

        return errors;
    }
}