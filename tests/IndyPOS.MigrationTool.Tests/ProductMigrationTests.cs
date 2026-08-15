using Dapper;
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
    public async Task MigrateProducts_Category_ResolvesTheLegacyIdToACatalogueCode()
    {
        // Defect 5 FIXED (was: Category = product.Category?.ToString(), the raw legacy id).
        // The legacy id is resolved through the store's OWN ProductCategory lookup table to a Thai
        // name, and that name to a catalogue code. Ids collide across store types -- 10 is
        // เบ็ดเตล็ด here and ของขวัญ in MimyShop -- so the id alone is not a mapping key; the name is.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var product = await db.Products.SingleAsync();

        product.Category.Should().Be(ProductCategoryCodes.GeneralMaterials,
            "legacy id 50 is วัสดุและอุปกรณ์ทั่วไป in this store, which is the GeneralMaterials code");
        product.Category.Should().NotBe("50", "the raw legacy id matches no catalogue code");
    }

    [Fact]
    public async Task MigrateProducts_WithACategoryIdTheStoreLookupDoesNotHave_MigratesUncategorisedAndReportsIt()
    {
        // A product pointing at a ProductCategory row that no longer exists. Losing the category is
        // not a reason to refuse the product's price, stock and sales history, so the row still
        // migrates -- but silently writing no category is what defect 5 felt like from the till, so
        // it is reported.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 999, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var product = await db.Products.SingleAsync();

        product.Category.Should().BeNull("the raw legacy id is never written as a fallback");
        result.Products.Migrated.Should().Be(1, "the product itself is fine");
        result.Errors.Should().ContainSingle()
              .Which.Should().Contain("999").And.Contain("8850001000010");
    }

    [Fact]
    public async Task MigrateProducts_WithACategoryNameTheCatalogueDoesNotHave_MigratesUncategorisedAndReportsIt()
    {
        // A category the shopkeeper added after LegacyCategoryMap was measured. Guessing a code
        // would file the product under a category nothing in the catalogue means -- the same harm
        // as the "Other" payment-method fallback defect 10 removed.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await store.Connection.ExecuteAsync(
            "INSERT INTO ProductCategory (Id, Category) VALUES (777, 'หมวดที่เพิ่งเพิ่ม')");
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 777, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var product = await db.Products.SingleAsync();

        product.Category.Should().BeNull();
        result.Errors.Should().ContainSingle()
              .Which.Should().Contain("หมวดที่เพิ่งเพิ่ม", "the operator needs the name to add it to the map");
    }

    [Fact]
    public async Task MigrateProducts_WithNoLegacyCategory_MigratesUncategorisedWithoutAnError()
    {
        // Category is nullable in the legacy schema and a genuinely uncategorised product is not a
        // data problem. Reporting it would bury the ones that ARE a problem under the error cap.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: null, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.Products.SingleAsync()).Category.Should().BeNull();
        result.Errors.Should().BeEmpty();
        result.Outcome.Should().Be(MigrationOutcome.Success);
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

    [Fact]
    public async Task MigrateProducts_WithNegativeLegacyStock_ClampsToZeroAndReportsIt()
    {
        // QuantityInStock was never strictly maintained: restocks often went unrecorded, so 952
        // GeneralHardware products and 583 MimyMart products sit at negative stock, down to -3,882.
        // Those are unrecorded restocks, not shelf state, so they are clamped to zero -- but the
        // clamp is a deliberate data change and must be reported, not silent. The operator needs
        // the list to drive a recount.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: -5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 11, barcode: "8850001000027", description: "Sand 25kg",
            unitPrice: 80m, quantityInStock: 12, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movements = await db.InventoryMovements.ToListAsync();

        // Both products must still migrate -- a clamp adjusts stock, it does not skip the product.
        (await db.Products.CountAsync()).Should().Be(2);

        movements.Should().ContainSingle("the clamped product gets no movement, so its stock is 0");
        movements.Single().QuantityDelta.Should().Be(12);

        var clamped = result.ClampedStocks.Should().ContainSingle().Subject;
        clamped.Barcode.Should().Be("8850001000010");
        clamped.ProductName.Should().Be("Cement 50kg");
        clamped.LegacyQuantity.Should().Be(-5);

        result.IsSuccess.Should().BeTrue(
            "a clamp is a reported data decision, not a row failure -- it must not change the outcome");
    }

    [Fact]
    public async Task MigrateProducts_InitialStockMovement_IsDatedAtMigrationTime()
    {
        // The quantity describes stock OBSERVED AT CUTOVER, not stock held when the product was
        // first created -- dating it 2021 would have any stock-over-time report claim the store
        // held today's inventory four years ago. One timestamp is captured per run, so every
        // product's movement shares it.
        var startedUtc = DateTime.UtcNow;

        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = await SeedCashierAsync(store);
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2021-06-01 09:00:00");
        await builder.AddProductAsync(
            productId: 11, barcode: "8850001000027", description: "Sand 25kg",
            unitPrice: 80m, quantityInStock: 12, category: 50, isTrackable: true,
            dateCreated: "2023-02-14 09:00:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var movements = await db.InventoryMovements.ToListAsync();
        var products = await db.Products.ToListAsync();

        movements.Should().HaveCount(2);

        // The products keep their own legacy dates -- only the movement moves.
        products.Select(p => p.CreatedUtc.Year).Should().BeEquivalentTo([2021, 2023]);

        foreach (var movement in movements)
        {
            movement.CreatedUtc.Should().BeOnOrAfter(startedUtc).And.BeOnOrBefore(DateTime.UtcNow);
        }

        // Documents the intent that one timestamp is captured per run. It cannot PROVE it: Windows
        // DateTime.UtcNow has coarse granularity, so an inline UtcNow per product would usually
        // produce identical values too. The BeOnOrAfter loop above is what actually catches the bug.
        movements.Select(m => m.CreatedUtc).Distinct().Should().ContainSingle();
    }
}
