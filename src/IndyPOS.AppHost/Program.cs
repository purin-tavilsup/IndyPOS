var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for local development
var postgres = builder.AddPostgres("postgres")
                      .WithPgAdmin(configureContainer: pgadmin => pgadmin.WithExplicitStart())
                      .WithDbGate(configureContainer: dbgate => dbgate.WithExplicitStart());

var storeHubDb = postgres.AddDatabase("storehub-db");
var cloudDb = postgres.AddDatabase("cloud-db");

// Cloud API (central cloud service) - must be defined first for service discovery
var cloudApi = builder.AddProject<Projects.IndyPOS_CloudApi>("cloud-api")
                      .WithReference(cloudDb)
                      .WaitFor(postgres);

// StoreHub API (local store service) - references CloudApi for sync
builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
       .WithReference(storeHubDb)
       .WithReference(cloudApi)
       .WaitFor(postgres)
       .WaitFor(cloudApi);

builder.Build().Run();
