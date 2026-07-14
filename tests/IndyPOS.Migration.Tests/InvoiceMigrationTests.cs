using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Migration.Tests;

/// <summary>
/// Tests for invoice migration from SQLite to PostgreSQL.
/// </summary>
[Collection("Migration")]
public class InvoiceMigrationTests : IAsyncLifetime
{
    private readonly MigrationTestFixture _fixture;
    private readonly MigrationService _migrationService;
    private const string TestStoreId = "test-store";

    public InvoiceMigrationTests(MigrationTestFixture fixture)
    {
        _fixture = fixture;
        _migrationService = new MigrationService();
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabasesAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrateInvoices_WithSingleInvoice_MigratesSuccessfully()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        // Seed user first
        var userId = SqliteTestDataSeeder.SeedUser(sqliteConnection, "Test", "User", 1, "testuser");
        var productId = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "TEST001", "Test Product", 100m, 50);

        // Seed invoice
        SqliteTestDataSeeder.SeedInvoice(sqliteConnection, userId, 200m,
        [
            new(productId, "TEST001", "Test Product", 2, 100m)
        ],
        [
            new(1, 200m, null) // Cash
        ]);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Migrate users first
        var userResult = await _migrationService.MigrateUsersAsync(sqliteConnection, postgresContext, TestStoreId);

        // Migrate products
        var productResult = await _migrationService.MigrateProductsAsync(sqliteConnection, postgresContext, TestStoreId);

        // Act - Migrate invoices
        var result = await _migrationService.MigrateInvoicesAsync(
            sqliteConnection, postgresContext, TestStoreId,
            productResult.ProductIdMap, userResult.UserIdMap);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Migrated.Should().Be(1);

        var migratedInvoice = await postgresContext.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync();

        migratedInvoice.Should().NotBeNull();
        migratedInvoice!.TotalAmount.Should().Be(200m);
        migratedInvoice.Lines.Should().HaveCount(1);
        migratedInvoice.Payments.Should().HaveCount(1);
    }

    [Fact]
    public async Task MigrateInvoices_WithMultipleLines_MigratesAllLines()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        var userId = SqliteTestDataSeeder.SeedUser(sqliteConnection, "Test", "User", 1, "testuser");
        var product1 = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "PROD001", "Product 1", 50m, 100);
        var product2 = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "PROD002", "Product 2", 75m, 100);
        var product3 = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "PROD003", "Product 3", 25m, 100);

        SqliteTestDataSeeder.SeedInvoice(sqliteConnection, userId, 225m,
        [
            new(product1, "PROD001", "Product 1", 2, 50m), // 100
            new(product2, "PROD002", "Product 2", 1, 75m), // 75
            new(product3, "PROD003", "Product 3", 2, 25m)  // 50
        ],
        [
            new(1, 225m, null)
        ]);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        var userResult = await _migrationService.MigrateUsersAsync(sqliteConnection, postgresContext, TestStoreId);
        var productResult = await _migrationService.MigrateProductsAsync(sqliteConnection, postgresContext, TestStoreId);

        // Act
        var result = await _migrationService.MigrateInvoicesAsync(
            sqliteConnection, postgresContext, TestStoreId,
            productResult.ProductIdMap, userResult.UserIdMap);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var invoice = await postgresContext.Invoices
            .Include(i => i.Lines)
            .FirstOrDefaultAsync();

        invoice.Should().NotBeNull();
        invoice!.Lines.Should().HaveCount(3);
    }

    [Fact]
    public async Task MigrateInvoices_WithMixedPayments_MapsPaymentTypes()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        var userId = SqliteTestDataSeeder.SeedUser(sqliteConnection, "Test", "User", 1, "testuser");
        var productId = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "TEST001", "Test", 100m, 50);

        SqliteTestDataSeeder.SeedInvoice(sqliteConnection, userId, 300m,
        [
            new(productId, "TEST001", "Test", 3, 100m)
        ],
        [
            new(1, 150m, null),          // Cash
            new(2, 100m, "Visa *1234"),  // Card
            new(3, 50m, "Transfer ref")  // Transfer
        ]);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        var userResult = await _migrationService.MigrateUsersAsync(sqliteConnection, postgresContext, TestStoreId);
        var productResult = await _migrationService.MigrateProductsAsync(sqliteConnection, postgresContext, TestStoreId);

        // Act
        var result = await _migrationService.MigrateInvoicesAsync(
            sqliteConnection, postgresContext, TestStoreId,
            productResult.ProductIdMap, userResult.UserIdMap);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var invoice = await postgresContext.Invoices
            .Include(i => i.Payments)
            .FirstOrDefaultAsync();

        invoice.Should().NotBeNull();
        invoice!.Payments.Should().HaveCount(3);
        invoice.Payments.Should().Contain(p => p.Method == "Cash" && p.Amount == 150m);
        invoice.Payments.Should().Contain(p => p.Method == "Card" && p.Amount == 100m);
        invoice.Payments.Should().Contain(p => p.Method == "Transfer" && p.Amount == 50m);
    }

    [Fact]
    public async Task MigrateInvoices_CreatesInventoryDeductions()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        var userId = SqliteTestDataSeeder.SeedUser(sqliteConnection, "Test", "User", 1, "testuser");
        var productId = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "STOCK001", "Stock Product", 50m, 100);

        SqliteTestDataSeeder.SeedInvoice(sqliteConnection, userId, 250m,
        [
            new(productId, "STOCK001", "Stock Product", 5, 50m)
        ],
        [
            new(1, 250m, null)
        ]);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        var userResult = await _migrationService.MigrateUsersAsync(sqliteConnection, postgresContext, TestStoreId);
        var productResult = await _migrationService.MigrateProductsAsync(sqliteConnection, postgresContext, TestStoreId);

        // Act
        var result = await _migrationService.MigrateInvoicesAsync(
            sqliteConnection, postgresContext, TestStoreId,
            productResult.ProductIdMap, userResult.UserIdMap);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var migratedProductId = productResult.ProductIdMap[productId];

        // Initial stock (100) + Sale deduction (-5) = 95
        var totalStock = await postgresContext.InventoryMovements
            .Where(m => m.ProductId == migratedProductId)
            .SumAsync(m => m.QuantityDelta);

        totalStock.Should().Be(95); // 100 initial - 5 sold
    }

    [Fact]
    public async Task MigrateInvoices_WithMultipleInvoices_MigratesAll()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        var userId = SqliteTestDataSeeder.SeedUser(sqliteConnection, "Test", "User", 1, "testuser");
        var productId = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "TEST001", "Test", 25m, 200);

        for (int i = 0; i < 5; i++)
        {
            SqliteTestDataSeeder.SeedInvoice(sqliteConnection, userId, 50m,
            [
                new(productId, "TEST001", "Test", 2, 25m)
            ],
            [
                new(1, 50m, null)
            ]);
        }

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        var userResult = await _migrationService.MigrateUsersAsync(sqliteConnection, postgresContext, TestStoreId);
        var productResult = await _migrationService.MigrateProductsAsync(sqliteConnection, postgresContext, TestStoreId);

        // Act
        var result = await _migrationService.MigrateInvoicesAsync(
            sqliteConnection, postgresContext, TestStoreId,
            productResult.ProductIdMap, userResult.UserIdMap);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Migrated.Should().Be(5);

        var invoiceCount = await postgresContext.Invoices.CountAsync();
        invoiceCount.Should().Be(5);
    }

    [Fact]
    public async Task MigrateInvoices_PreservesProductName()
    {
        // Arrange - Product name might change after invoice, but invoice should keep original
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        var userId = SqliteTestDataSeeder.SeedUser(sqliteConnection, "Test", "User", 1, "testuser");
        var productId = SqliteTestDataSeeder.SeedProduct(sqliteConnection, "RENAME001", "Original Name", 100m, 50);

        SqliteTestDataSeeder.SeedInvoice(sqliteConnection, userId, 100m,
        [
            new(productId, "RENAME001", "Original Name At Time Of Sale", 1, 100m)
        ],
        [
            new(1, 100m, null)
        ]);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        var userResult = await _migrationService.MigrateUsersAsync(sqliteConnection, postgresContext, TestStoreId);
        var productResult = await _migrationService.MigrateProductsAsync(sqliteConnection, postgresContext, TestStoreId);

        // Act
        var result = await _migrationService.MigrateInvoicesAsync(
            sqliteConnection, postgresContext, TestStoreId,
            productResult.ProductIdMap, userResult.UserIdMap);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var invoiceLine = await postgresContext.InvoiceLines.FirstOrDefaultAsync();
        invoiceLine.Should().NotBeNull();
        invoiceLine!.ProductName.Should().Be("Original Name At Time Of Sale");
    }
}
