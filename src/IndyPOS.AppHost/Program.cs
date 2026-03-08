var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for local development
var postgres = builder.AddPostgres("postgres")
    .WithPgAdmin();

var storeHubDb = postgres.AddDatabase("storehub");

// StoreHub API
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub")
    .WithReference(storeHubDb)
    .WaitFor(postgres);

builder.Build().Run();
