var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for local development
var postgres = builder.AddPostgres("postgres")
                      .WithPgAdmin()
                      .WithDbGate();

var storeHubDb = postgres.AddDatabase("storehub-db");

// StoreHub API
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
       .WithReference(storeHubDb)
       .WaitFor(postgres);

builder.Build().Run();
