using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Compares the committed schema artefacts against a real store database when one is present.
///
/// The .db files are gitignored and ~64 MB, so these cannot run in CI. They therefore SKIP
/// loudly rather than passing silently: the previous version returned early, so 8 tests reported
/// Passed on every machine but one.
///
/// The skip is decided by <see cref="RealStoreFactAttribute"/> at discovery time, because
/// xUnit 2.9.3 has no runtime skip.
/// </summary>
public class RealStoreSchemaTests
{
    private static async Task<List<string>> ColumnsAsync(string dbPath, string table)
    {
        await using var connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        await connection.OpenAsync();
        return (await connection.QueryAsync<string>(
            $"SELECT name FROM pragma_table_info('{table}')")).ToList();
    }

    private static async Task<List<string>> ArtefactColumnsAsync(LegacyStoreShape shape, string table)
    {
        var ddl = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "LegacySchema", $"{shape}.sql"));

        await using var connection = new SQLiteConnection("Data Source=:memory:;Version=3;");
        await connection.OpenAsync();
        await connection.ExecuteAsync(ddl);
        return (await connection.QueryAsync<string>(
            $"SELECT name FROM pragma_table_info('{table}')")).ToList();
    }

    public static TheoryData<LegacyStoreShape, string> ShapesAndTables()
    {
        var data = new TheoryData<LegacyStoreShape, string>();
        foreach (var table in new[]
                 {
                     "User", "UserCredential", "UserRole", "InventoryProduct", "Invoice",
                     "InvoiceProduct", "Payment", "PaymentType", "ProductCategory",
                     "ProductBarcodeCounter"
                 })
        {
            data.Add(LegacyStoreShape.GeneralHardware, table);
            data.Add(LegacyStoreShape.MimyShop, table);
        }

        // PayLater exists only in the GeneralHardware shape -- that asymmetry IS defect 3.
        data.Add(LegacyStoreShape.GeneralHardware, "PayLater");
        return data;
    }

    [RealStoreTheory]
    [MemberData(nameof(ShapesAndTables))]
    public async Task Artefact_ShouldHaveExactlyTheRealStoresColumns(LegacyStoreShape shape, string table)
    {
        var real = await ColumnsAsync(RealStoreDatabases.PathFor(shape), table);
        var artefact = await ArtefactColumnsAsync(shape, table);

        real.Should().NotBeEmpty($"the real {shape} store must actually have a {table} table");
        artefact.Should().BeEquivalentTo(real,
            $"the committed {shape}.sql artefact must match the real store's {table} exactly. " +
            "If this fails, regenerate it: dotnet test --filter \"ExtractLegacySchema\"");
    }

    [RealStoreFact(LegacyStoreShape.GeneralHardware)]
    public async Task RealStore_EveryPaymentTypeInUse_ShouldMapToACatalogueCode()
    {
        await using var connection = new SQLiteConnection(
            $"Data Source={RealStoreDatabases.PathFor(LegacyStoreShape.GeneralHardware)};Version=3;");
        await connection.OpenAsync();

        var idsInUse = (await connection.QueryAsync<long>(
            "SELECT DISTINCT PaymentTypeId FROM Payment ORDER BY PaymentTypeId")).ToList();

        idsInUse.Should().NotBeEmpty("the sample store has payment history");
        idsInUse.Where(id => LegacyPaymentTypeMap.ToCode((int)id) is null).Should().BeEmpty(
            "every legacy payment type present in real store data must map to a catalogue code");
    }

    [RealStoreFact(LegacyStoreShape.GeneralHardware)]
    public async Task RealStore_PaymentTypeLabels_ShouldStillMatchTheAssumedMapping()
    {
        await using var connection = new SQLiteConnection(
            $"Data Source={RealStoreDatabases.PathFor(LegacyStoreShape.GeneralHardware)};Version=3;");
        await connection.OpenAsync();

        var labels = (await connection.QueryAsync<(long Id, string Type)>(
            "SELECT Id, Type FROM PaymentType")).ToDictionary(r => (int)r.Id, r => r.Type);

        labels[1].Should().Be("เงินสด");
        labels[2].Should().Be("ลงบัญชี");
        labels[3].Should().Be("บัตรสวัสดิการแห่งรัฐ");
        labels[4].Should().Be("ม.33");
        labels[5].Should().Be("โอนเข้าบัญชี");
        // Id 6 has no catalogue equivalent. Pinned anyway: it is the one row that a check covering
        // only the mappable ids would never notice going wrong.
        labels[6].Should().Be("ผ่อนชำระ");
        labels[7].Should().Be("คนละครึ่ง");
        labels[8].Should().Be("เราชนะ");
    }

    [RealStoreFact(LegacyStoreShape.GeneralHardware)]
    public async Task RealStore_PayLaterShouldExtendPayment_OneToOne()
    {
        // The structural basis for defect 10. If a future store's data breaks this, the
        // link-instead-of-create fix needs revisiting.
        await using var connection = new SQLiteConnection(
            $"Data Source={RealStoreDatabases.PathFor(LegacyStoreShape.GeneralHardware)};Version=3;");
        await connection.OpenAsync();

        var orphans = await connection.ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM PayLater pl
            LEFT JOIN Payment p ON p.PaymentId = pl.PaymentId
            WHERE p.PaymentId IS NULL
            """);
        var wrongType = await connection.ExecuteScalarAsync<long>("""
            SELECT COUNT(*) FROM PayLater pl
            JOIN Payment p ON p.PaymentId = pl.PaymentId
            WHERE p.PaymentTypeId <> 2
            """);

        orphans.Should().Be(0, "every PayLater row must extend an existing Payment");
        wrongType.Should().Be(0, "every linked payment must be legacy type 2 (ลงบัญชี)");
    }
}
