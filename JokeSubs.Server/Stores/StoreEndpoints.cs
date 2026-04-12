using Microsoft.AspNetCore.Http.HttpResults;

namespace JokeSubs.Server.Stores;

public static class StoreEndpoints
{
    public static IEndpointRouteBuilder MapStoreEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/stores")
            .WithTags("Stores");

        group.MapGet("/{id}", async Task<Results<Ok<Store>, NotFound>> (string id, IStoreRepository storeRepository) =>
        {
            var store = await storeRepository.GetByIdAsync(id);
            return store is null ? TypedResults.NotFound() : TypedResults.Ok(store);
        })
        .WithName("GetStoreById");

        group.MapGet("", async (IStoreRepository storeStore) =>
            TypedResults.Ok(await storeStore.GetAllAsync()))
            .WithName("GetStores");

        group.MapPost("", async Task<Results<Created<Store>, ValidationProblem>> (CreateStoreRequest request, IStoreRepository storeStore) =>
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

        group.MapPost("/{id}/groups", async Task<Results<Ok<Store>, NotFound, ValidationProblem>> (
            string id,
            AddStoreGroupRequest request,
            IStoreRepository storeRepository) =>
        {
            var store = await storeRepository.GetByIdAsync(id);
            if (store is null)
            {
                return TypedResults.NotFound();
            }

            var errors = ValidateGroupRequest(request, store);
            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var updatedStore = await storeRepository.AddGroupAsync(store.Id, request);
            return updatedStore is null
                ? TypedResults.NotFound()
                : TypedResults.Ok(updatedStore);
        })
        .WithName("AddStoreGroup");

        return app;
    }

    private static async Task<Dictionary<string, string[]>> ValidateRequest(CreateStoreRequest request, IStoreRepository store)
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

    private static Dictionary<string, string[]> ValidateGroupRequest(AddStoreGroupRequest request, Store store)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var trimmedName = request.Name?.Trim() ?? string.Empty;

        if (trimmedName.Length == 0)
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }
        else if (store.Groups.Any(group =>
            group.Name.Equals(trimmedName, StringComparison.OrdinalIgnoreCase)))
        {
            errors[nameof(request.Name)] = ["Group name must be unique within the store."];
        }

        return errors;
    }
}