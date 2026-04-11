#pragma warning disable ASPIRECOSMOSDB001
var builder = DistributedApplication.CreateBuilder(args);

var cosmos = builder.AddAzureCosmosDB("cosmos")
    .RunAsPreviewEmulator(emulator => emulator
        .WithDataExplorer()
        .WithLifetime(ContainerLifetime.Persistent));

var db = cosmos.AddCosmosDatabase("jokesubs");
var storesContainer = db.AddContainer("stores", "/id");

var server = builder.AddProject<Projects.JokeSubs_Server>("server")
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints()
    .WithReference(storesContainer)
    .WaitFor(cosmos);

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
