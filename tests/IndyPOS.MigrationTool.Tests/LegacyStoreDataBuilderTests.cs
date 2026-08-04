using Dapper;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Guards the builder itself. No PostgreSQL and no migrator involved -- these assert that the
/// fixture reproduces the legacy database's real quirks.
/// </summary>
public class LegacyStoreDataBuilderTests
{
    [Fact]
    public async Task AddInvoiceLine_ShouldLeaveIsTrackableAtItsSqliteDefault_Defect7Addendum()
    {
        // Defect 7 addendum. InvoiceProduct.IsTrackable is DEAD DATA in every real store:
        // 602,114 lines, every one 1, not a single 0, because the legacy INSERT omits the column
        // and SQLite applies DEFAULT 1. A migration that restores a per-line trackable flag by
        // reading this column marks every service line stock-tracked -- the exact bug defect 7
        // exists to prevent.
        //
        // This test fails if the builder ever starts setting the column, which would silently
        // restore the blindness the whole harness exists to remove.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);

        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 4242, barcode: "2002500000014", description: "Delivery service",
            unitPrice: 50m, quantityInStock: 0, category: 25, isTrackable: false,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 50m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 4242, barcode: "2002500000014",
            description: "Delivery service", quantity: 1, unitPrice: 50m, originalUnitPrice: 50m);

        var lineFlag = await store.Connection.ExecuteScalarAsync<long>(
            "SELECT IsTrackable FROM InvoiceProduct WHERE InvoiceProductId = 1");
        var productFlag = await store.Connection.ExecuteScalarAsync<long>(
            "SELECT IsTrackable FROM InventoryProduct WHERE InventoryProductId = 4242");

        lineFlag.Should().Be(1, "the legacy INSERT omits the column, so SQLite applies DEFAULT 1");
        productFlag.Should().Be(0, "the product is genuinely non-trackable, and that column IS maintained");
    }

    [Fact]
    public async Task AddPaymentTypeLookup_ShouldSeedTheEightRealRows()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);

        await builder.AddPaymentTypeLookupAsync();

        var labels = (await store.Connection.QueryAsync<(long Id, string Type)>(
            "SELECT Id, Type FROM PaymentType ORDER BY Id")).ToDictionary(r => (int)r.Id, r => r.Type);

        labels.Should().HaveCount(8);
        labels[1].Should().Be("เงินสด");
        labels[2].Should().Be("ลงบัญชี");
        labels[3].Should().Be("บัตรสวัสดิการแห่งรัฐ");
        labels[4].Should().Be("ม.33");
        labels[5].Should().Be("โอนเข้าบัญชี");
        // Id 6 has NO catalogue equivalent, which is exactly why its label must be pinned: it is the
        // one row a test asserting only "mappable ids" would never notice going wrong.
        labels[6].Should().Be("ผ่อนชำระ");
        labels[7].Should().Be("คนละครึ่ง");
        labels[8].Should().Be("เราชนะ");
    }
}
