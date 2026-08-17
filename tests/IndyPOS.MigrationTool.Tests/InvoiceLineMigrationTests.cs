using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class InvoiceLineMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public InvoiceLineMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MigrateInvoiceLines_OriginalUnitPrice_IsDeliberatelyNotMigrated()
    {
        // Defect 6, and this test's PREMISE WAS WRONG until 2026-08-17. It used to claim that
        // "165,690 of 325,780 real lines (51%) were discounted, and the record of that is lost",
        // and called OriginalUnitPrice the fix.
        //
        // Measured across all three real stores, that 165,690 is the count of rows where
        // OriginalUnitPrice is ZERO -- unset -- not discounted. Partitioned properly:
        //
        //                              GeneralHardware   MimyMart   MimyShop
        //   OriginalUnitPrice = 0               165,716    276,317         17
        //   = UnitPrice (non-zero)              160,064          0          0
        //   > UnitPrice (a real discount)             0          0          0
        //
        // Every row is 0 or exactly UnitPrice. Total discount recorded anywhere: THB 0.00. There is
        // no discount history to preserve, so the column is NOT migrated and InvoiceLine gains no
        // OriginalUnitPrice -- adding one would create a field that looks like discount history and
        // is not, which a later report would reasonably trust.
        //
        // This assertion therefore stays as it was, but it is now pinning a DECISION rather than
        // recording a defect. If a store ever starts recording real discounts, this is the test to
        // change, and the partition above is the measurement to redo first.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Discounted item",
            unitPrice: 100m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 11, barcode: "8850001000011", description: "Full price item",
            unitPrice: 80m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 160m, dateCreated: "2024-03-15 14:30:00");

        // Discounted: originally 100, sold at 80.
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Discounted item", quantity: 1, unitPrice: 80m, originalUnitPrice: 100m);

        // Never discounted: always 80.
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 2, invoiceId: 1, productId: 11, barcode: "8850001000011",
            description: "Full price item", quantity: 1, unitPrice: 80m, originalUnitPrice: 80m);

        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 160m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var lines = await db.InvoiceLines.OrderBy(l => l.ProductName).ToListAsync();

        lines.Should().HaveCount(2);
        lines.Select(l => l.UnitPrice).Should().AllBeEquivalentTo(80m);
        lines.Select(l => l.LineTotal).Should().AllBeEquivalentTo(80m);

        typeof(IndyPOS.Domain.Entities.Core.InvoiceLine)
            .GetProperties().Select(p => p.Name)
            .Should().NotContain("OriginalUnitPrice",
                "no real store records a discount in it, so the field would promise history v4 does " +
                "not have");
    }

    [Fact]
    public async Task MigrateInvoiceLines_PreservesNoteGroupPriceAndPriority()
    {
        // Defect 6 FIXED for the three columns that carry information v4 does not already hold.
        // Measured before choosing, across the three real stores:
        //
        //   Note        68,904 non-empty in GeneralHardware, 11,473 distinct (+8,276 MimyMart)
        //   Priority    every line, and exactly 1..n on 138,324 of 139,680 invoices -- line order
        //   GroupPrice  sparse but real: 93 rows GeneralHardware, 555 MimyMart
        //
        // IsGroupProduct is NOT migrated: 15 rows in GeneralHardware and 0 anywhere else, and
        // MimyMart has 555 group prices with the flag never set, so the flag never recorded the
        // intent reliably. See MigrateInvoiceLines_IsGroupProduct_IsDeliberatelyNotMigrated.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Screws",
            unitPrice: 20m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 54m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Screws", quantity: 3, unitPrice: 18m, originalUnitPrice: 20m,
            groupPrice: 54m, isGroupProduct: true, note: "ลดราคาให้ลูกค้าประจำ", priority: 2,
            category: 50);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 54m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var line = await db.InvoiceLines.SingleAsync();

        line.Quantity.Should().Be(3);
        line.UnitPrice.Should().Be(18m);

        line.Note.Should().Be("ลดราคาให้ลูกค้าประจำ",
            "Note is free text the till captured and nothing else in v4 holds");
        line.Priority.Should().Be(2, "Priority is the line's position on its invoice");
        line.GroupPrice.Should().Be(54m);
    }

    [Fact]
    public async Task MigrateInvoiceLines_WithNoNoteGroupPriceOrPriority_LeavesThemUnset()
    {
        // The legacy defaults are 0 and empty rather than NULL, and 0 is not a group price. Writing
        // 0 would make "sold at a group price of nothing" indistinguishable from "not a group sale".
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Screws",
            unitPrice: 20m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 20m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Screws", quantity: 1, unitPrice: 20m, originalUnitPrice: 20m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 20m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var line = await db.InvoiceLines.SingleAsync();

        line.Note.Should().BeNull("an empty legacy note is absence, not an empty remark");
        line.GroupPrice.Should().BeNull("0 is not a group price");
        line.Priority.Should().BeNull("0 is not a position on the invoice");
    }

    [Fact]
    public void MigrateInvoiceLines_IsGroupProduct_IsDeliberatelyNotMigrated()
    {
        // Defect 6's fifth column, deliberately dropped rather than restored. Measured: 15 rows set
        // in GeneralHardware, 0 in MimyMart and 0 in MimyShop -- while MimyMart carries 555 non-zero
        // GroupPrice values with the flag never set. So the flag does not record whether a line was
        // a group sale; GroupPrice does that better by simply being present.
        //
        // Same shape as InvoiceProduct.IsTrackable, which is dead for the same reason (see
        // LegacyStoreDataBuilderTests.AddInvoiceLine_ShouldLeaveIsTrackableAtItsSqliteDefault).
        typeof(IndyPOS.Domain.Entities.Core.InvoiceLine)
            .GetProperties().Select(p => p.Name)
            .Should().NotContain("IsGroupProduct");
    }

    [Fact]
    public async Task MigrateInvoiceLines_WithADeletedProduct_PreservesTheLineAgainstAPlaceholder()
    {
        // Defect 13 (FIXED). Real GeneralHardware had 1,980 invoice lines whose product no longer
        // exists; they were skipped with a warning, so an invoice's lines silently summed to less
        // than its own total. ProductName is a historical snapshot, so the line survives without
        // the product.
        // This is the tier-3 case: nothing live shares the barcode, so a placeholder product is
        // synthesised. It is inactive, so it stays out of pickers and the active catalogue.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 80m, dateCreated: "2024-03-15 14:30:00");
        // References InventoryProductId 777, which is never inserted.
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 777, barcode: "8850009999999",
            description: "Product deleted years ago", quantity: 1, unitPrice: 80m,
            originalUnitPrice: 80m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 80m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.Invoices.CountAsync()).Should().Be(1);

        var line = await db.InvoiceLines.SingleAsync();
        line.ProductName.Should().Be("Product deleted years ago",
            "ProductName is the historical snapshot and must not be rewritten");
        line.UnitPrice.Should().Be(80m);
        line.Quantity.Should().Be(1);

        var placeholder = await db.Products.SingleAsync();
        placeholder.Barcode.Should().Be("8850009999999");
        placeholder.IsActive.Should().BeFalse("a resurrected product must stay out of the catalogue");
        line.ProductId.Should().Be(placeholder.Id);

        // The harm this defect caused: the invoice no longer reconciled against its own lines.
        var invoice = await db.Invoices.SingleAsync();
        invoice.TotalAmount.Should().Be(80m);
        (line.UnitPrice * line.Quantity).Should().Be(invoice.TotalAmount);
    }

    [Fact]
    public async Task MigrateInvoiceLines_WithADeletedProductWhoseBarcodeIsLive_RelinksToTheLiveProduct()
    {
        // Defect 13, tier 2 -- the majority case. 1,736 of the 1,980 real orphaned lines carry a
        // barcode that a LIVE product still has: the shopkeeper deleted the product and re-added it
        // under a new id with a tidied-up name ('M 150 150 มล.' -> 'เครื่องดื่ม M150 Original').
        // A barcode identifies the physical article, so the line relinks to that product rather
        // than resurrecting a duplicate -- which also keeps one article's sales history in one place.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");

        // The re-added product: new legacy id, same barcode, tidied name.
        await builder.AddProductAsync(
            productId: 3834, barcode: "8851123212021", description: "เครื่องดื่ม M150 Original",
            unitPrice: 12m, quantityInStock: 40, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 12m, dateCreated: "2024-03-15 14:30:00");

        // The historical line still points at the DELETED id 38, under its old name.
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 38, barcode: "8851123212021",
            description: "M 150 150 มล.", quantity: 1, unitPrice: 12m, originalUnitPrice: 12m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 12m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.Products.CountAsync()).Should().Be(1,
            "the article already exists, so no placeholder may be synthesised for it");

        var live = await db.Products.SingleAsync();
        var line = await db.InvoiceLines.SingleAsync();

        line.ProductId.Should().Be(live.Id, "the line relinks to the product sharing its barcode");
        line.ProductName.Should().Be("M 150 150 มล.",
            "the line keeps the name the customer was actually charged under, not today's name");
    }
}
