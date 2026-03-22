using JokeSubs.Server.Locations;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

builder.AddAzureCosmosClient("locations",
    configureClientOptions: options =>
    {
        options.SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        };
    });

builder.Services.AddSingleton<ILocationStore, CosmosLocationStore>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapLocationEndpoints();

app.UseFileServer();

app.Run();
