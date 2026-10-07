using FluentAssertions;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Testing.Postgres;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Migrations;

/// <summary>
/// Development once built its database with EnsureCreated, which leaves tables but no migration history.
/// Migrating such a database replays the first migration and dies on "relation already exists", so
/// Development refuses it up front, with a message that says what to do.
/// </summary>
public class EnsureCreatedDatabaseTests : IClassFixture<EnsureCreatedDatabaseTests.ServerFixture>
{
    private readonly ServerFixture _server;

    public EnsureCreatedDatabaseTests(ServerFixture server) => _server = server;

    [Fact]
    public async Task RejectEnsureCreatedDatabase_WithAnEnsureCreatedDatabase_ThrowsNamingTheCause()
    {
        await using var db = NewContext(await _server.Postgres.CreateDatabaseAsync());
        await db.Database.EnsureCreatedAsync();

        var act = () => StoreHubDbContextExtensions.RejectEnsureCreatedDatabaseAsync(db);

        await act.Should()
                 .ThrowAsync<InvalidOperationException>()
                 .WithMessage("*EnsureCreated*");
    }

    [Fact]
    public async Task RejectEnsureCreatedDatabase_WithAMigratedDatabase_DoesNotThrow()
    {
        await using var db = NewContext(await _server.Postgres.CreateDatabaseAsync());
        await db.Database.MigrateAsync();

        var act = () => StoreHubDbContextExtensions.RejectEnsureCreatedDatabaseAsync(db);

        await act.Should()
                 .NotThrowAsync();
    }

    [Fact]
    public async Task RejectEnsureCreatedDatabase_WithAnEmptyDatabase_DoesNotThrow()
    {
        await using var db = NewContext(await _server.Postgres.CreateDatabaseAsync());

        var act = () => StoreHubDbContextExtensions.RejectEnsureCreatedDatabaseAsync(db);

        await act.Should()
                 .NotThrowAsync();
    }

    // The proof behind the check: without it, migrating an EnsureCreated database fails, and not clearly.
    [Fact]
    public async Task Migrate_WithAnEnsureCreatedDatabase_FailsOnAnExistingTable()
    {
        await using var db = NewContext(await _server.Postgres.CreateDatabaseAsync());
        await db.Database.EnsureCreatedAsync();

        var act = () => db.Database.MigrateAsync();

        (await act.Should()
                  .ThrowAsync<Npgsql.PostgresException>()).Which.SqlState.Should()
                                                          .Be("42P07");
    }

    private static StoreHubDbContext NewContext(string connectionString) =>
        new(new DbContextOptionsBuilder<StoreHubDbContext>().UseNpgsql(connectionString)
                                                           .Options);

    public sealed class ServerFixture : IAsyncLifetime
    {
        public TestPostgres Postgres { get; private set; } = null!;

        public async Task InitializeAsync() => Postgres = await TestPostgres.StartAsync();

        public async Task DisposeAsync() => await Postgres.DisposeAsync();
    }
}
