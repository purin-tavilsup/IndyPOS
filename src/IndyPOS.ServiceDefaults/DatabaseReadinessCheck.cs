using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;

namespace IndyPOS.ServiceDefaults;

/// <summary>
/// The one readiness check a service has: a <c>SELECT 1</c> against its own database. StoreHub's must
/// never include the cloud, so it can serve the till while the cloud is unreachable.
/// </summary>
public static class DatabaseReadinessCheck
{
    public const string Name = "database";

    private const int TimeoutSeconds = 3;

    public static TBuilder AddDatabaseReadinessCheck<TBuilder>(this TBuilder builder, string connectionName)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddHealthChecks()
                        .AddNpgSql(services => BoundedConnectionString(services, connectionName),
                                   name: Name,
                                   tags: [Extensions.ReadyTag],
                                   timeout: TimeSpan.FromSeconds(TimeoutSeconds));

        return builder;
    }

    // Read on the first check, then cached by the package: by then UnprotectSecrets (production),
    // Aspire (dev) or a test factory has put the real value in IConfiguration. Npgsql's connect phase
    // ignores cancellation, so only its own Timeout bounds a hanging server; it overrides whatever
    // the string carries, for this check only. A missing string throws and the probe answers 500.
    private static string BoundedConnectionString(IServiceProvider services, string connectionName)
    {
        var configured = services.GetRequiredService<IConfiguration>().GetConnectionString(connectionName)
                         ?? throw new InvalidOperationException($"Connection string '{connectionName}' is not configured.");

        return new NpgsqlConnectionStringBuilder(configured)
        {
            Timeout = TimeoutSeconds,
            CommandTimeout = TimeoutSeconds
        }.ConnectionString;
    }
}
