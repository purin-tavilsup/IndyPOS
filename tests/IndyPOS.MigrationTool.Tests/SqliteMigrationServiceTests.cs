using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.TestData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class SqliteMigrationServiceTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private SQLiteConnection _sqliteConnection = null!;
    private string _sqliteDbPath = null!;

    public SqliteMigrationServiceTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        // Create temp SQLite database
        _sqliteDbPath = Path.Combine(Path.GetTempPath(), $"migration_test_{Guid.NewGuid()}.db");
        _sqliteConnection = new SQLiteConnection($"Data Source={_sqliteDbPath};Version=3;");
        await _sqliteConnection.OpenAsync();

        // Reset PostgreSQL
        await _postgres.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqliteConnection.CloseAsync();
        _sqliteConnection.Dispose();

        // Clean up temp file
        if (File.Exists(_sqliteDbPath))
        {
            File.Delete(_sqliteDbPath);
        }
    }

    [Fact]
    public async Task MigrateAllAsync_WithEmptyDatabase_ReturnsSuccess()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.CreateSchemaAsync();

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TotalMigrated.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateAllAsync_WithUsers_MigratesAllUsers()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        var data = await seeder.SeedCompleteDataSetAsync(userCount: 5, productCount: 0, invoiceCount: 0);

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Users.Migrated.Should().Be(5);
        result.Users.Failed.Should().Be(0);

        // Verify in PostgreSQL
        await using var context = _postgres.CreateDbContext();
        var pgUsers = await context.StoreUsers.CountAsync();
        pgUsers.Should().Be(5);
    }

    [Fact]
    public async Task MigrateAllAsync_WithProducts_MigratesAllProducts()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        var data = await seeder.SeedCompleteDataSetAsync(userCount: 1, productCount: 25, invoiceCount: 0);

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Products.Migrated.Should().Be(25);

        // Verify in PostgreSQL
        await using var context = _postgres.CreateDbContext();
        var pgProducts = await context.Products.CountAsync();
        pgProducts.Should().Be(25);

        // Verify initial inventory movements were created
        var movements = await context.InventoryMovements
            .Where(m => m.Reason == "Migration:InitialStock")
            .CountAsync();
        movements.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MigrateAllAsync_WithInvoices_MigratesAllInvoicesAndLines()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        var data = await seeder.SeedCompleteDataSetAsync(userCount: 2, productCount: 10, invoiceCount: 20);

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Invoices.Migrated.Should().Be(20);

        // Verify in PostgreSQL
        await using var context = _postgres.CreateDbContext();
        var pgInvoices = await context.Invoices.CountAsync();
        pgInvoices.Should().Be(20);

        // Verify invoice lines exist
        var pgLines = await context.InvoiceLines.CountAsync();
        pgLines.Should().BeGreaterThan(0);

        // Verify payments exist
        var pgPayments = await context.Payments.CountAsync();
        pgPayments.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task MigrateAllAsync_WithPayLater_MigratesPayLaterRecords()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        var data = await seeder.SeedCompleteDataSetAsync(userCount: 2, productCount: 10, invoiceCount: 50);

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify PayLater records in PostgreSQL
        await using var context = _postgres.CreateDbContext();
        var pgPayLaters = await context.PayLaters.CountAsync();

        // PayLater count should match SQLite
        pgPayLaters.Should().Be(data.PayLaterCount);
    }

    [Fact]
    public async Task MigrateAllAsync_WithCompleteDataset_PreservesTotalAmounts()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        var data = await seeder.SeedCompleteDataSetAsync(userCount: 3, productCount: 20, invoiceCount: 50);

        // Calculate SQLite totals
        var sqliteTotalRevenue = await _sqliteConnection.ExecuteScalarAsync<decimal>(
            "SELECT COALESCE(SUM(Total), 0) FROM Invoice");

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify totals match in PostgreSQL
        await using var context = _postgres.CreateDbContext();
        var pgTotalRevenue = await context.Invoices.SumAsync(i => i.TotalAmount);

        pgTotalRevenue.Should().BeApproximately(sqliteTotalRevenue, 0.10m); // Allow for decimal precision differences
    }

    [Fact]
    public async Task MigrateAllAsync_CalledTwice_SkipsUsersAndProducts()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.SeedCompleteDataSetAsync(userCount: 3, productCount: 10, invoiceCount: 0); // No invoices for this test

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act - First migration
        var result1 = await service.MigrateAllAsync();

        // Act - Second migration (should skip existing)
        var result2 = await service.MigrateAllAsync();

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result2.IsSuccess.Should().BeTrue();

        // Second run should skip users (matched by username) and products (matched by barcode)
        result2.Users.Skipped.Should().Be(result1.Users.Migrated);
        result2.Products.Skipped.Should().Be(result1.Products.Migrated);

        // Total count in PostgreSQL should not have duplicates
        await using var context = _postgres.CreateDbContext();
        var userCount = await context.StoreUsers.CountAsync();
        var productCount = await context.Products.CountAsync();

        userCount.Should().Be(3, "users should not be duplicated");
        productCount.Should().Be(10, "products should not be duplicated");
    }

    [Fact]
    public async Task MigrateAllAsync_DryRun_DoesNotWriteToDatabase()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.SeedCompleteDataSetAsync(userCount: 3, productCount: 10, invoiceCount: 20);

        var options = CreateOptions(dryRun: true);
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.Users.Migrated.Should().BeGreaterThan(0);
        result.Products.Migrated.Should().BeGreaterThan(0);

        // But PostgreSQL should be empty
        await using var context = _postgres.CreateDbContext();
        var userCount = await context.StoreUsers.CountAsync();
        var productCount = await context.Products.CountAsync();

        userCount.Should().Be(0);
        productCount.Should().Be(0);
    }

    [Fact]
    public async Task MigrateAllAsync_WithProductGroupPricing_PreservesGroupPrice()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.CreateSchemaAsync();

        // Add product with group pricing
        await _sqliteConnection.ExecuteAsync("""
            INSERT INTO InventoryProduct (Barcode, Description, UnitPrice, QuantityInStock, GroupPrice, GroupPriceQuantity, DateCreated)
            VALUES ('1234567890', 'Test Product', 100.00, 50, 270.00, 3, '2024-01-01 10:00:00');
            """);

        // Add user for the migration to work
        await _sqliteConnection.ExecuteAsync("""
            INSERT INTO User (FirstName, LastName, RoleId, DateCreated) VALUES ('Test', 'User', 1, '2024-01-01');
            INSERT INTO UserCredential (UserId, Username, Password) VALUES (1, 'test', 'hash');
            """);

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();

        await using var context = _postgres.CreateDbContext();
        var product = await context.Products.FirstAsync();

        product.UnitPrice.Should().Be(100.00m);
        product.GroupPrice.Should().Be(270.00m);
        product.GroupPriceQuantity.Should().Be(3);
    }

    [Fact]
    public async Task MigrateAllAsync_WithUserWithoutCredentials_SkipsUser()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.CreateSchemaAsync();

        // Add user WITHOUT credentials
        await _sqliteConnection.ExecuteAsync("""
            INSERT INTO User (FirstName, LastName, RoleId, DateCreated)
            VALUES ('No', 'Credentials', 1, '2024-01-01');
            """);

        var options = CreateOptions();
        var service = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);

        // Act
        var result = await service.MigrateAllAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Users.Skipped.Should().Be(1);
        result.Users.Migrated.Should().Be(0);
    }

    private MigrationOptions CreateOptions(bool dryRun = false)
    {
        return new MigrationOptions
        {
            SqlitePath = _sqliteDbPath,
            PostgresConnectionString = _postgres.ConnectionString,
            StoreId = "TEST-STORE",
            DryRun = dryRun
        };
    }
}
