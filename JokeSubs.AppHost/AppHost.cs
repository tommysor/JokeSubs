#pragma warning disable ASPIRECOSMOSDB001
var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator => emulator.WithDataExplorer());

var db = cosmos.AddCosmosDatabase("jokesubs");
var locationsContainer = db.AddContainer("locations", "/id");

var server = builder.AddProject<Projects.JokeSubs_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(locationsContainer)
    .WaitFor(cosmos);

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
