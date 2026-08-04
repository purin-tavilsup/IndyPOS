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
    public async Task MigrateInvoiceLines_CurrentlyCannotDistinguishADiscountedLine_Defect6()
    {
        // Defect 6: the InvoiceProduct SELECT takes 7 of 17 columns, dropping OriginalUnitPrice,
        // GroupPrice, IsGroupProduct, Note and Priority. 165,690 of 325,780 real lines (51%) were
        // discounted, and the record of that is lost.
        // CORRECT: the discount is recoverable -- which needs a v4 schema change, since InvoiceLine
        // is deliberately 7 fields. That is why this is pinned here and fixed in its own spec.
        //
        // Two lines, same sold price. One was discounted from 100 to 80, the other always cost 80.
        // After migration they are indistinguishable, so no report can ever recompute the discount.
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

        // The migrated rows differ only by product. Nothing records that ฿20 was given away.
        typeof(IndyPOS.Domain.Entities.Core.InvoiceLine)
            .GetProperties().Select(p => p.Name)
            .Should().NotContain("OriginalUnitPrice",
                "defect 6: with no such field, a discounted line and a full-price line are identical");
    }

    [Fact]
    public async Task MigrateInvoiceLines_CurrentlyDiscardsNoteAndGroupPricing_Defect6()
    {
        // Defect 6, the other four dropped columns. CORRECT: all of them preserved.
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

        var fields = typeof(IndyPOS.Domain.Entities.Core.InvoiceLine)
            .GetProperties().Select(p => p.Name).ToList();

        fields.Should().NotContain("GroupPrice");
        fields.Should().NotContain("IsGroupProduct");
        fields.Should().NotContain("Note", "the cashier's reason for the discount is lost");
        fields.Should().NotContain("Priority");
    }

    [Fact]
    public async Task MigrateInvoiceLines_WithADeletedProduct_CurrentlySkipsTheLine()
    {
        // Defect 13. Real GeneralHardware has 1,980 invoice lines whose product no longer exists.
        // The line is skipped with a warning, so an invoice's lines can silently sum to less than
        // its total.
        // CORRECT: the line is preserved -- ProductName is already a historical snapshot.
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
        (await db.InvoiceLines.CountAsync()).Should().Be(0,
            "the line is dropped, so the invoice total no longer matches the sum of its lines");
        (await db.Invoices.SingleAsync()).TotalAmount.Should().Be(80m);
    }
}
