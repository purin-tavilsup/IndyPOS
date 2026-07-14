using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IndyPOS.Migration.Tests;

/// <summary>
/// Tests for product migration from SQLite to PostgreSQL.
/// </summary>
[Collection("Migration")]
public class ProductMigrationTests : IAsyncLifetime
{
    private readonly MigrationTestFixture _fixture;
    private readonly MigrationService _migrationService;
    private const string TestStoreId = "test-store";

    public ProductMigrationTests(MigrationTestFixture fixture)
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
    public async Task MigrateProducts_WithSingleProduct_MigratesSuccessfully()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "8850999111001",
            description: "Test Product",
            unitPrice: 99.99m,
            quantityInStock: 50,
            manufacturer: "TestMfg",
            brand: "TestBrand",
            category: 1);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Migrated.Should().Be(1);
        result.Skipped.Should().Be(0);
        result.Failed.Should().Be(0);

        var migratedProduct = await postgresContext.Products
            .FirstOrDefaultAsync(p => p.Barcode == "8850999111001");

        migratedProduct.Should().NotBeNull();
        migratedProduct!.Name.Should().Be("Test Product");
        migratedProduct.UnitPrice.Should().Be(99.99m);
        migratedProduct.Manufacturer.Should().Be("TestMfg");
        migratedProduct.Brand.Should().Be("TestBrand");
        migratedProduct.Category.Should().Be("1");
        migratedProduct.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task MigrateProducts_WithStock_CreatesInventoryMovement()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "STOCK001",
            description: "Product With Stock",
            unitPrice: 50m,
            quantityInStock: 100);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var product = await postgresContext.Products
            .FirstOrDefaultAsync(p => p.Barcode == "STOCK001");
        product.Should().NotBeNull();

        // Verify initial stock movement was created
        var totalStock = await postgresContext.InventoryMovements
            .Where(m => m.ProductId == product!.Id)
            .SumAsync(m => m.QuantityDelta);

        totalStock.Should().Be(100);
    }

    [Fact]
    public async Task MigrateProducts_WithZeroStock_NoInventoryMovement()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "NOSTOCK001",
            description: "No Stock Product",
            unitPrice: 25m,
            quantityInStock: 0);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var product = await postgresContext.Products
            .FirstOrDefaultAsync(p => p.Barcode == "NOSTOCK001");
        product.Should().NotBeNull();

        var movementCount = await postgresContext.InventoryMovements
            .Where(m => m.ProductId == product!.Id)
            .CountAsync();

        movementCount.Should().Be(0);
    }

    [Fact]
    public async Task MigrateProducts_WithGroupPrice_MigratesGroupPricing()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "GROUP001",
            description: "Group Price Product",
            unitPrice: 30m,
            quantityInStock: 200,
            groupPrice: 50m,
            groupPriceQuantity: 2);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var product = await postgresContext.Products
            .FirstOrDefaultAsync(p => p.Barcode == "GROUP001");

        product.Should().NotBeNull();
        product!.GroupPrice.Should().Be(50m);
        product.GroupPriceQuantity.Should().Be(2);
    }

    [Fact]
    public async Task MigrateProducts_WithUnicodeCharacters_PreservesText()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "UNICODE001",
            description: "ไทย 日本語 中文 한국어",
            unitPrice: 100m,
            quantityInStock: 10,
            manufacturer: "ผู้ผลิตไทย",
            brand: "แบรนด์ไทย");

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var product = await postgresContext.Products
            .FirstOrDefaultAsync(p => p.Barcode == "UNICODE001");

        product.Should().NotBeNull();
        product!.Name.Should().Be("ไทย 日本語 中文 한국어");
        product.Manufacturer.Should().Be("ผู้ผลิตไทย");
        product.Brand.Should().Be("แบรนด์ไทย");
    }

    [Fact]
    public async Task MigrateProducts_WithMultipleProducts_MigratesAll()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        for (int i = 1; i <= 10; i++)
        {
            SqliteTestDataSeeder.SeedProduct(sqliteConnection,
                barcode: $"MULTI{i:D3}",
                description: $"Product {i}",
                unitPrice: 10m * i,
                quantityInStock: i * 5);
        }

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Migrated.Should().Be(10);

        var productCount = await postgresContext.Products.CountAsync();
        productCount.Should().Be(10);
    }

    [Fact]
    public async Task MigrateProducts_DuplicateBarcode_SkipsExisting()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "DUP001",
            description: "First Product",
            unitPrice: 50m);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Run first migration
        await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Run second migration (same data)
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Skipped.Should().Be(1);
        result.Migrated.Should().Be(0);

        var productCount = await postgresContext.Products
            .Where(p => p.Barcode == "DUP001")
            .CountAsync();
        productCount.Should().Be(1);
    }

    [Fact]
    public async Task MigrateProducts_WithNullFields_HandlesGracefully()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "NULL001",
            description: "Minimal Product",
            unitPrice: 25m,
            manufacturer: null,
            brand: null,
            category: null);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();

        var product = await postgresContext.Products
            .FirstOrDefaultAsync(p => p.Barcode == "NULL001");

        product.Should().NotBeNull();
        product!.Manufacturer.Should().BeNull();
        product.Brand.Should().BeNull();
        product.Category.Should().BeNull();
    }

    [Fact]
    public async Task MigrateProducts_GeneratesNewGuids()
    {
        // Arrange
        using var sqliteConnection = _fixture.CreateSqliteConnection();
        sqliteConnection.Open();

        SqliteTestDataSeeder.SeedProduct(sqliteConnection,
            barcode: "GUID001",
            description: "GUID Test Product",
            unitPrice: 100m);

        await using var postgresContext = _fixture.CreatePostgresDbContext();

        // Act
        var result = await _migrationService.MigrateProductsAsync(
            sqliteConnection, postgresContext, TestStoreId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.ProductIdMap.Should().NotBeEmpty();

        var product = await postgresContext.Products
            .FirstOrDefaultAsync(p => p.Barcode == "GUID001");

        product.Should().NotBeNull();
        product!.Id.Should().NotBeEmpty();
    }
}
