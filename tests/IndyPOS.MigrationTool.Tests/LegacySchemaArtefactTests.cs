using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Guards the generated legacy schema artefacts. If one of these fails, the artefact was
/// hand-edited or regenerated from something that is not a real store.
/// </summary>
public class LegacySchemaArtefactTests
{
    private static async Task<SQLiteConnection> ApplyAsync(LegacyStoreShape shape)
    {
        var ddl = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "LegacySchema", $"{shape}.sql"));

        var connection = new SQLiteConnection("Data Source=:memory:;Version=3;");
        await connection.OpenAsync();
        await connection.ExecuteAsync(ddl);
        return connection;
    }

    private static async Task<List<string>> TablesAsync(SQLiteConnection connection) =>
        (await connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name"))
        .ToList();

    private static async Task<List<string>> ColumnsAsync(SQLiteConnection connection, string table) =>
        (await connection.QueryAsync<string>($"SELECT name FROM pragma_table_info('{table}')")).ToList();

    [Fact]
    public async Task GeneralHardwareArtefact_ShouldDeclareTheThirteenRealTables()
    {
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        (await TablesAsync(db)).Should().BeEquivalentTo([
            "Customers", "Installments", "InventoryProduct", "Invoice", "InvoiceProduct",
            "PayLater", "Payment", "PaymentType", "ProductBarcodeCounter", "ProductCategory",
            "User", "UserCredential", "UserRole"
        ]);
    }

    [Fact]
    public async Task MimyShopArtefact_ShouldDeclareTenTablesAndNoPayLater()
    {
        await using var db = await ApplyAsync(LegacyStoreShape.MimyShop);

        var tables = await TablesAsync(db);

        tables.Should().BeEquivalentTo([
            "InventoryProduct", "Invoice", "InvoiceProduct", "Payment", "PaymentType",
            "ProductBarcodeCounter", "ProductCategory", "User", "UserCredential", "UserRole"
        ]);
        tables.Should().NotContain("PayLater",
            "the MimyShop shape exists precisely to exercise a store with no PayLater feature");
    }

    [Fact]
    public async Task InvoiceProduct_ShouldHaveAllSeventeenRealColumns()
    {
        // Defect 6: the migrator SELECTs only 7 of these. The five that carry the discount and
        // group-pricing record are OriginalUnitPrice, GroupPrice, IsGroupProduct, Note, Priority.
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        (await ColumnsAsync(db, "InvoiceProduct")).Should().BeEquivalentTo([
            "InvoiceProductId", "Priority", "InvoiceId", "InventoryProductId", "Barcode",
            "Description", "Manufacturer", "Brand", "Category", "Quantity", "IsTrackable",
            "DateCreated", "Note", "UnitPrice", "GroupPrice", "IsGroupProduct", "OriginalUnitPrice"
        ]);
    }

    [Fact]
    public async Task PayLater_ShouldHaveTheRealColumns_NotTheOnesTheMigratorAsksFor()
    {
        // Defect 2. The migrator SELECTs PayLaterId, UserId, CustomerName and PaymentAmount.
        // None of them exist: PayLater is a 1:1 extension of Payment, so its PK IS the payment's id.
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        var columns = await ColumnsAsync(db, "PayLater");

        columns.Should().BeEquivalentTo([
            "PaymentId", "Description", "InvoiceId", "IsCompleted",
            "DateCreated", "DateUpdated", "PayLaterAmount", "PaidAmount"
        ]);
        columns.Should().NotContain("PayLaterId");
        columns.Should().NotContain("UserId");
        columns.Should().NotContain("CustomerName");
        columns.Should().NotContain("PaymentAmount");
    }

    [Fact]
    public async Task PayLater_PrimaryKeyShouldBePaymentId_ProvingItExtendsPayment()
    {
        await using var db = await ApplyAsync(LegacyStoreShape.GeneralHardware);

        var pk = (await db.QueryAsync<string>(
            "SELECT name FROM pragma_table_info('PayLater') WHERE pk > 0")).ToList();

        pk.Should().BeEquivalentTo(["PaymentId"],
            "PayLater is table-per-subtype on Payment: Payment generates the id, PayLater receives it");
    }

    [Fact]
    public async Task LegacyStoreDatabase_ShouldCreateARealFileWithTheRequestedShape()
    {
        await using var store = await Fixtures.LegacyStoreDatabase.CreateAsync(LegacyStoreShape.MimyShop);

        File.Exists(store.Path).Should().BeTrue();

        var tables = (await store.Connection.QueryAsync<string>(
            "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'")).ToList();

        tables.Should().HaveCount(10);
        tables.Should().NotContain("PayLater");
    }
}
