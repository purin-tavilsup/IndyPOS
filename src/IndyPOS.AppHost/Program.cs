using IndyPOS.StoreProfiles;

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL for local development
// WithDataVolume persists data across restarts and reuses containers
// WithLifetime(ContainerLifetime.Persistent) keeps container running after Aspire stops
var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume("indypos-postgres-data")
                      .WithLifetime(ContainerLifetime.Persistent)
                      .WithPgAdmin(configureContainer: pgadmin => pgadmin.WithExplicitStart());

// The cloud database is "cloud": the old "cloud-db" was built by EnsureCreated and cannot be migrated.
var cloudDb = postgres.AddDatabase("cloud-db", databaseName: "cloud");

// Cloud API (central cloud service) - must be defined first for service discovery
var cloudApi = builder.AddProject<Projects.IndyPOS_CloudApi>("cloud-api")
                      .WithReference(cloudDb)
                      .WithHttpHealthCheck("/health/ready", endpointName: "http")
                      .WaitFor(postgres);

// Dev stores: --store <GeneralHardware|MimyMart|MimyShop|all>; none = GeneralHardware.
// Each store gets its own database, StoreHub and till. See IndyPOS.StoreProfiles.
var stores = StoreProfiles.Resolve(builder.Configuration["store"]);
var single = stores.Count == 1;

foreach (var profile in stores)
{
    // Resource names share one namespace with the StoreHub resources, so the database resource gets a
    // "-db" suffix; the database itself is named after the store.
    var database = postgres.AddDatabase($"{profile.DatabaseName}-db", databaseName: profile.DatabaseName);

    // No launch profile: three instances of one project would all claim its https port.
    var storeHub = builder.AddProject<Projects.IndyPOS_StoreHub>(single ? "storehub-api" : $"storehub-{profile.Key.ToLowerInvariant()}",
                                                                 launchProfileName: null)
                          .WithHttpEndpoint(port: profile.DevPort, name: "http")
                          .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
                          .WithEnvironment("Store__Type", profile.Type.ToString())
                          .WithEnvironment("Store__Id", profile.StoreId)
                          .WithEnvironment("Store__Name", profile.Name)
                          .WithEnvironment("Store__Code", profile.Code.ToString())
                          .WithEnvironment("Secrets__Directory", DevStoreFiles.SecretsDirectory(builder.AppHostDirectory, profile))
                          .WithEnvironment("CloudApi__ClientId", profile.CloudClientId)
                          .WithEnvironment("CloudApi__ClientSecret", StoreProfiles.DevCloudClientSecret)
                          .WithReference(database, "storehub-db")
                          .WithReference(cloudApi)
                          .WithHttpHealthCheck("/health/ready", endpointName: "http")
                          .WaitFor(postgres);

    // A plain string: an interpolated literal here would be read as an Aspire reference expression.
    var storeHubUrl = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"http://localhost:{profile.DevPort}");
    builder.AddProject<Projects.IndyPOS_Windows_Forms>(single ? "winforms-app" : $"till-{profile.Key.ToLowerInvariant()}")
           .WithReference(storeHub)
           .WithEnvironment("StoreHub__BaseUrl", storeHubUrl)
           .WithEnvironment("Store__ConfigPath", DevStoreFiles.StoreConfigurationPath(builder.AppHostDirectory, profile))
           .WaitFor(storeHub)
           .WithExplicitStart();
}

builder.Build().Run();
