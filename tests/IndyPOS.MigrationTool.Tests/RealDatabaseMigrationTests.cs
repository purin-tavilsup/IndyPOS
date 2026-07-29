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
    public async Task RealDatabase_EveryPaymentTypeInUse_ShouldMapToACatalogueCode()
    {
        // The guard the scrambled mapping needed. Reads the ids this store actually uses and
        // requires each to resolve, so a store whose data contains an id the map does not know
        // fails here rather than during a cutover - or worse, silently, as "Other".
        if (!SampleDatabaseExists())
        {
            return;
        }

        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        var idsInUse = (await conn.QueryAsync<long>(
            "SELECT DISTINCT PaymentTypeId FROM Payment ORDER BY PaymentTypeId")).ToList();

        idsInUse.Should().NotBeEmpty("the sample store has payment history");

        var unmapped = idsInUse
            .Where(id => Services.LegacyPaymentTypeMap.ToCode((int)id) is null)
            .ToList();

        unmapped.Should().BeEmpty(
            "every legacy payment type present in real store data must map to a catalogue code");
    }

    [Fact]
    public async Task RealDatabase_PaymentTypeLabels_ShouldStillMatchTheAssumedMapping()
    {
        // Pins the mapping to the store's OWN lookup table rather than to a translation done
        // once in a review. If a label ever moves to a different id, this fails loudly.
        if (!SampleDatabaseExists())
        {
            return;
        }

        await using var conn = new SQLiteConnection($"Data Source={RealDbPath};Version=3;");
        await conn.OpenAsync();

        var labels = (await conn.QueryAsync<(long Id, string Type)>(
                "SELECT Id, Type FROM PaymentType"))
            .ToDictionary(r => (int)r.Id, r => r.Type);

        labels[1].Should().Be("เงินสด");                    // Cash
        labels[2].Should().Be("ลงบัญชี");                    // PayLater
        labels[3].Should().Be("บัตรสวัสดิการแห่งรัฐ");        // WelfareCard
        labels[5].Should().Be("โอนเข้าบัญชี");                // MoneyTransfer
        labels[7].Should().Be("คนละครึ่ง");                  // FiftyFifty
        labels[8].Should().Be("เราชนะ");                     // WeWin
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
