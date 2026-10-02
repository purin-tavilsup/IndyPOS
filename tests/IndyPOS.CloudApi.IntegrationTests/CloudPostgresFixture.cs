using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using IndyPOS.Testing.Postgres;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// One PostgreSQL server per test class, and a fresh migrated database per test, so tests share
/// the slow server start but never each other's rows.
///
/// Needs Docker, or INDYPOS_TEST_POSTGRES pointing at a server (see TestPostgres).
/// </summary>
public sealed class CloudPostgresFixture : IAsyncLifetime
{
    private TestPostgres? _postgres;

    public async Task InitializeAsync() => _postgres = await TestPostgres.StartAsync();

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await _postgres!.CreateDatabaseAsync();

        await using var db = CreateContext(connectionString);
        await db.Database.MigrateAsync();

        return connectionString;
    }

    /// <summary>
    /// Configured as Aspire's AddNpgsqlDbContext configures it: with the Npgsql retrying execution
    /// strategy on. InMemory has no such strategy, which is how a bare user transaction shipped.
    /// </summary>
    public static void Configure(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure());

    public static CloudDbContext CreateContext(string connectionString, params IInterceptor[] interceptors)
    {
        var options = new DbContextOptionsBuilder<CloudDbContext>();
        Configure(options, connectionString);
        options.AddInterceptors(interceptors);

        return new CloudDbContext(options.Options);
    }
}
