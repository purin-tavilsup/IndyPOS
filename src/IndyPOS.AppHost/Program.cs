var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for local development
var postgres = builder.AddPostgres("postgres")
                      .WithPgAdmin(configureContainer: pgadmin => pgadmin.WithExplicitStart())
                      .WithDbGate(configureContainer: dbgate => dbgate.WithExplicitStart());

var storeHubDb = postgres.AddDatabase("storehub-db");
var cloudDb = postgres.AddDatabase("cloud-db");

// StoreHub API (local store service)
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
       .WithReference(storeHubDb)
       .WaitFor(postgres);

// Cloud API (central cloud service)
builder.AddProject<Projects.IndyPOS_CloudApi>("cloud-api")
       .WithReference(cloudDb)
       .WaitFor(postgres);

builder.Build().Run();
