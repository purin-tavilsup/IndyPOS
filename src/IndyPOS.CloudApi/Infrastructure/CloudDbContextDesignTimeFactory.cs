namespace IndyPOS.CloudApi.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Lets `dotnet ef` build the model without executing Program.cs, whose Aspire component demands a
/// real "cloud-db" connection string and whose startup guard demands a configured signing key.
///
/// The fallback connection string below is never connected to — generating a migration needs only
/// the provider, so that Npgsql decides the column types. `dotnet ef database update` (and any other
/// design-time operation that does need a real connection) can override it with the same
/// ConnectionStrings__cloud-db environment variable Program.cs would otherwise read via Aspire.
/// </summary>
public sealed class CloudDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CloudDbContext>
{
    public CloudDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("cloud-db")
            ?? "Host=design-time-only;Database=indypos_cloud;Username=none;Password=none";

        var options = new DbContextOptionsBuilder<CloudDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CloudDbContext(options);
    }
}
