using IndyPOS.Cloud.Auth.Data;
using IndyPOS.Testing.Postgres;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Cloud.Auth.IntegrationTests;

/// <summary>
/// One PostgreSQL per test class; each test asks for a fresh, migrated Auth database. Needs Docker, or
/// INDYPOS_TEST_POSTGRES pointing at a server (see TestPostgres).
/// </summary>
public sealed class AuthPostgresFixture : IAsyncLifetime
{
    private TestPostgres? _postgres;

    public async Task InitializeAsync() => _postgres = await TestPostgres.StartAsync();

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
            await _postgres.DisposeAsync();
    }

    public async Task<string> CreateDatabaseAsync()
    {
        var connectionString = await _postgres!.CreateDatabaseAsync();

        await using var db = CreateContext(connectionString);
        await db.Database.MigrateAsync();

        return connectionString;
    }

    public static AuthDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(connectionString).Options);
}
