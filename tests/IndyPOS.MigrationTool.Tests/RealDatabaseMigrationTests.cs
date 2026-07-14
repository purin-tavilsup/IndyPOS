using System.Data.SQLite;
using Dapper;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Schema verification tests for the real SQLite database.
/// Heavy migration tests are skipped - use SqliteMigrationServiceTests with Bogus data instead.
/// </summary>
public class RealDatabaseSchemaTests
{
    private const string RealDbPath = @"C:\personal\IndyPOS\.planning\indypos-overhaul\sqlite_database\Store.db";

    private bool SampleDatabaseExists() => File.Exists(RealDbPath);

    [Fact]
    public async Task RealDatabase_HasExpectedSchema()
    {
        // Skip if sample database doesn't exist
        if (!SampleDatabaseExists())
        {
            return;
        }

        // Arrange
        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        // Act - check for expected tables
        var tables = (await conn.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")).ToList();

        // Assert - verify all required tables exist
        tables.Should().Contain("User");
        tables.Should().Contain("UserCredential");
        tables.Should().Contain("InventoryProduct");
        tables.Should().Contain("Invoice");
        tables.Should().Contain("InvoiceProduct");
        tables.Should().Contain("Payment");
        tables.Should().Contain("PayLater");
    }

    [Fact]
    public async Task RealDatabase_HasData()
    {
        // Skip if sample database doesn't exist
        if (!SampleDatabaseExists())
        {
            return;
        }

        // Arrange
        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        // Act
        var userCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM User");
        var productCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM InventoryProduct");
        var invoiceCount = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Invoice");

        // Assert - real database should have data
        userCount.Should().BeGreaterThan(0, "Real database should have users");
        productCount.Should().BeGreaterThan(0, "Real database should have products");
        invoiceCount.Should().BeGreaterThan(0, "Real database should have invoices");
    }

    [Fact]
    public async Task RealDatabase_UserTable_HasExpectedColumns()
    {
        if (!SampleDatabaseExists()) return;

        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        var columns = (await conn.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('User')")).ToList();

        columns.Should().Contain("UserId");
        columns.Should().Contain("FirstName");
        columns.Should().Contain("LastName");
        columns.Should().Contain("RoleId");
    }

    [Fact]
    public async Task RealDatabase_ProductTable_HasExpectedColumns()
    {
        if (!SampleDatabaseExists()) return;

        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        var columns = (await conn.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('InventoryProduct')")).ToList();

        columns.Should().Contain("InventoryProductId");
        columns.Should().Contain("Barcode");
        columns.Should().Contain("Description");
        columns.Should().Contain("UnitPrice");
        columns.Should().Contain("QuantityInStock");
        columns.Should().Contain("GroupPrice");
        columns.Should().Contain("GroupPriceQuantity");
    }

    [Fact]
    public async Task RealDatabase_InvoiceTable_HasExpectedColumns()
    {
        if (!SampleDatabaseExists()) return;

        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        var columns = (await conn.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('Invoice')")).ToList();

        columns.Should().Contain("InvoiceId");
        columns.Should().Contain("UserId");
        columns.Should().Contain("Total");
        columns.Should().Contain("DateCreated");
    }

    [Fact]
    public async Task RealDatabase_PaymentTable_HasExpectedColumns()
    {
        if (!SampleDatabaseExists()) return;

        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        var columns = (await conn.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('Payment')")).ToList();

        columns.Should().Contain("PaymentId");
        columns.Should().Contain("InvoiceId");
        columns.Should().Contain("PaymentTypeId");
        columns.Should().Contain("Amount");
    }
}
