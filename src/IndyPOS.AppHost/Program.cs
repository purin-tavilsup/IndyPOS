var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for local development
// WithDataVolume persists data across restarts and reuses containers
// WithLifetime(ContainerLifetime.Persistent) keeps container running after Aspire stops
var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume("indypos-postgres-data")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithPgAdmin(configureContainer: pgadmin => pgadmin.WithExplicitStart());

var storeHubDb = postgres.AddDatabase("storehub-db");
var cloudDb = postgres.AddDatabase("cloud-db");

// Cloud API (central cloud service) - must be defined first for service discovery
var cloudApi = builder.AddProject<Projects.IndyPOS_CloudApi>("cloud-api")
                      .WithReference(cloudDb)
                      .WaitFor(postgres);

// StoreHub API (local store service) - references CloudApi for sync
var storeHub = builder.AddProject<Projects.IndyPOS_StoreHub>("storehub-api")
                      .WithReference(storeHubDb)
                      .WithReference(cloudApi)
                      .WaitFor(postgres)
                      .WaitFor(cloudApi);

// WinForms desktop app - explicit start so it doesn't auto-launch.
// appsettings.json defaults StoreHub to :5000 (the installed/production port);
// override to the Aspire dev port (:5012) here so dev and prod can differ
// without shipping a dev value to customers.
builder.AddProject<Projects.IndyPOS_Windows_Forms>("winforms-app")
       .WithReference(storeHub)
       .WithEnvironment("StoreHub__BaseUrl", "http://localhost:5012")
       .WaitFor(storeHub)
       .WithExplicitStart();

builder.Build().Run();
