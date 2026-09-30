using IndyPOS.CloudApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// One PostgreSQL container per test class, and a fresh migrated database per test, so tests share
/// the slow container start but never each other's rows.
///
/// Requires Docker (Testcontainers spins up postgres:16-alpine).
/// </summary>
public sealed class CloudPostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    public async Task<string> CreateDatabaseAsync()
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString())
        {
            Database = $"cloud_{Guid.NewGuid():N}"
        }.ConnectionString;

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

    public static CloudDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<CloudDbContext>();
        Configure(options, connectionString);

        return new CloudDbContext(options.Options);
    }
}
