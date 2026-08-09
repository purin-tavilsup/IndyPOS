using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class ProductMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public ProductMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<LegacyStoreDataBuilder> SeedCashierAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        return builder;
    }

    [Fact]
    public async Task MigrateProducts_Category_CurrentlyWritesRawLegacyId_Defect5()
    {
        // Defect 5: Category = product.Category?.ToString() writes the raw legacy id.
        // CORRECT: ProductCategoryCodes.GeneralMaterials ("GeneralMaterials"), resolved from
        // legacy id 50 via the store-scoped product_category catalogue Epic 1 added.
        // A raw id matches no catalogue code, so every migrated product is uncategorised and both
        // the Hardware gate and the category pickers break.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var product = await db.Products.SingleAsync();

        product.Category.Should().Be("50");
        product.Category.Should().NotBe(ProductCategoryCodes.GeneralMaterials,
            "this is the correct answer and defect 5 does not yet produce it");
    }

    [Fact]
    public async Task MigrateProducts_CurrentlyPreservesNoLegacyId_Defect8()
    {
        // Defect 8: only StoreUser carries a legacy id (LegacyUserId). Products, invoices, lines
        // and payments do not, so the migration cannot be re-run idempotently by id and a v4 row
        // cannot be reconciled against its SQLite source.
        // CORRECT: a legacy id preserved on all five entity types.
        // This test asserts the structural fact: StoreUser has the property and Product has no
        // equivalent. Product's absence cannot be checked at compile time, so it is checked by
        // reflection below -- which only flips if the eventual fix names the property exactly
        // "LegacyProductId". A fix naming it LegacyId or SourceProductId leaves this pin green, so
        // whoever fixes defect 8 must invert this pin deliberately rather than rely on it failing.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 4242, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.StoreUsers.SingleAsync()).LegacyUserId.Should().Be(1,
            "users DO preserve their legacy id");

        // The product's legacy id 4242 survives only in this in-memory map, which is discarded when
        // the process exits. Nothing in PostgreSQL records it.
        result.ProductIdMap.Should().ContainKey(4242);

        var productProperties = typeof(IndyPOS.Domain.Entities.Core.Product)
            .GetProperties().Select(p => p.Name).ToList();
        productProperties.Should().NotContain("LegacyProductId",
            "defect 8: Product has no legacy id column, so a migrated row cannot be reconciled");
    }

    [Fact]
    public async Task MigrateProducts_WithANumericPrice_PreservesTheValue()
    {
        // Covers that GroupPrice and GroupPriceQuantity are mapped AT ALL -- they are the two money
        // columns nothing else asserts.
        //
        // It does NOT prove the NUMERIC -> double -> decimal hop is lossless: measured on .NET 10,
        // (decimal)19.99d == 19.99m and (decimal)269.97d == 269.97m both hold exactly, so these
        // values can never detect a precision loss. Pick a value that does not round-trip in binary
        // floating point if you want to test that. If this test FAILS it is a NEW defect: record it,
        // do not weaken the assertion to match the observed value.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Screws, box of 100",
            unitPrice: 19.99m, quantityInStock: 5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00", groupPrice: 269.97m, groupPriceQuantity: 15);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var product = await db.Products.SingleAsync();

        product.UnitPrice.Should().Be(19.99m);
        product.GroupPrice.Should().Be(269.97m);
        product.GroupPriceQuantity.Should().Be(15);
    }

    [Fact]
    public async Task MigrateProducts_WithStock_ShouldRecordAnInitialStockMovement()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movement = await db.InventoryMovements
            .SingleAsync(m => m.Reason == "Migration:InitialStock");

        movement.QuantityDelta.Should().Be(20);
    }

    [Fact]
    public async Task MigrateProducts_DoesNotReplaySalesAgainstCurrentStock_Defect14()
    {
        // Defect 14 (fixed 2026-08-09): QuantityInStock is TODAY's stock -- already net of every
        // sale the store ever made -- so replaying historical invoice lines as stock movements
        // subtracted every sold unit twice. Measured before the fix: GeneralHardware netted
        // -400,541 units with 7,028 of 10,590 products (66%) negative; MimyMart -249,652 with
        // 2,350 of 6,335 (37%). Product has no stock column: InventoryMovement.cs:6 defines stock
        // as SUM(QuantityDelta).
        // The migration now writes ONE movement per product and none for historical lines. If this
        // test fails with a Migration:Sale movement present, the replay has come back.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 600m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 5, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 600m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movements = await db.InventoryMovements.ToListAsync();

        movements.Should().ContainSingle().Which.Reason.Should().Be("Migration:InitialStock");
        movements.Sum(m => m.QuantityDelta).Should().Be(20,
            "migrated stock must equal what the old till displayed");

        // The sale itself is not lost -- it is the invoice line.
        (await db.InvoiceLines.CountAsync()).Should().Be(1);
    }
}
