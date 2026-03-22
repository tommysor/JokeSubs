using Microsoft.AspNetCore.Http.HttpResults;

namespace JokeSubs.Server.Locations;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/locations")
            .WithTags("Locations");

        group.MapGet("", async (ILocationStore store) =>
            TypedResults.Ok(await store.GetAllAsync()))
            .WithName("GetLocations");

        group.MapPost("", async Task<Results<Created<Location>, ValidationProblem>> (CreateLocationRequest request, ILocationStore store) =>
        {
            var errors = await ValidateRequest(request, store);

            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(errors);
            }

            var location = await store.AddAsync(request);
            return TypedResults.Created($"/api/locations/{Uri.EscapeDataString(location.Id)}", location);
        })
        .WithName("CreateLocation");

        return app;
    }

    private static async Task<Dictionary<string, string[]>> ValidateRequest(CreateLocationRequest request, ILocationStore store)
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