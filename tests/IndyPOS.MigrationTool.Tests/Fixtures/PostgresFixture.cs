using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using IndyPOS.Testing.Postgres;

namespace IndyPOS.MigrationTool.Tests.Fixtures;

/// <summary>
/// Shared PostgreSQL fixture (container or CI server) for migration tests.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private TestPostgres? _postgres;

    public string ConnectionString => _postgres!.ConnectionString;

    public async Task InitializeAsync()
    {
        _postgres = await TestPostgres.StartAsync();

        // Apply EF Core migrations
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new StoreHubDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }

    /// <summary>
    /// Creates a fresh DbContext for assertions.
    /// </summary>
    public StoreHubDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new StoreHubDbContext(options);
    }

    /// <summary>
    /// Resets the database by deleting all data from tables.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await using var context = CreateDbContext();

        // Delete data in correct order (FK constraints)
        context.PayLaters.RemoveRange(context.PayLaters);
        context.InventoryMovements.RemoveRange(context.InventoryMovements);
        context.Payments.RemoveRange(context.Payments);
        context.InvoiceLines.RemoveRange(context.InvoiceLines);
        context.Invoices.RemoveRange(context.Invoices);
        context.Products.RemoveRange(context.Products);
        context.StoreUsers.RemoveRange(context.StoreUsers);

        await context.SaveChangesAsync();
    }
}

[CollectionDefinition("Postgres")]
public class PostgresCollection : ICollectionFixture<PostgresFixture>
{
}
