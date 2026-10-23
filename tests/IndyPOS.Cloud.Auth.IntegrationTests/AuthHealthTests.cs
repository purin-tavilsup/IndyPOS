using System.Net;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace IndyPOS.Cloud.Auth.IntegrationTests;

/// <summary>Auth follows the API conventions' health routes: live never touches the database, ready checks its own.</summary>
public class AuthHealthTests(AuthPostgresFixture postgres) : IClassFixture<AuthPostgresFixture>
{
    [Fact]
    public async Task HealthReady_WithItsDatabase_ReturnsHealthy()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());

        var response = await host.CreateClient().GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthLive_ReturnsHealthy()
    {
        await using var host = new AuthTestHost(await postgres.CreateDatabaseAsync());

        var response = await host.CreateClient().GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AuthDatabase_AfterMigrating_HasStoreClientsAndOpenIddictTables()
    {
        await using var connection = new NpgsqlConnection(await postgres.CreateDatabaseAsync());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            """select string_agg(table_name, ',' order by table_name) from information_schema.tables where table_schema = 'public' and table_name <> '__EFMigrationsHistory'""",
            connection);

        var tables = (string)(await command.ExecuteScalarAsync())!;

        tables.Should().Be("OpenIddictApplications,OpenIddictAuthorizations,OpenIddictScopes,OpenIddictTokens,StoreClients");
    }
}
