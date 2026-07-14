using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace IndyPOS.MigrationTool.Tests.Fixtures;

/// <summary>
/// Shared PostgreSQL container fixture for migration tests.
/// </summary>
public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public string ConnectionString => _container.GetConnectionString();

    public PostgresFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("indypos_test")
            .WithUsername("test")
            .WithPassword("test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply EF Core migrations
        var options = new DbContextOptionsBuilder<StoreHubDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using var context = new StoreHubDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
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
