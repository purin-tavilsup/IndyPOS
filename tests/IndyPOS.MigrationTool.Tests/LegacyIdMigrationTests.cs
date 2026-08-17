using FluentAssertions;
using IndyPOS.MigrationTool;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Defect 8: every migrated row records the legacy id it came from.
/// </summary>
/// <remarks>
/// Only <c>StoreUser</c> used to. Without it a v4 row cannot be reconciled against its SQLite source,
/// and for invoices there is no natural key to fall back on — measured, GeneralHardware holds invoices
/// that share <c>(UserId, Total, DateCreated)</c>.
/// <para>
/// ⚠️ This delivers RECONCILIATION, not idempotency. The invoice, line and payment phases still do not
/// skip what they already migrated, so a second run still duplicates them and the operator docs still
/// say never to re-run.
/// </para>
/// </remarks>
[Collection("Postgres")]
public class LegacyIdMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public LegacyIdMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task SeedOneSaleAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 4242, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(7001, userId: 1, total: 120m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 8001, invoiceId: 7001, productId: 4242, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 1, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddPaymentAsync(
            paymentId: 9001, invoiceId: 7001, paymentTypeId: 1, amount: 120m,
            dateCreated: "2024-03-15 14:30:00");
    }

    [Fact]
    public async Task MigrateAll_RecordsTheLegacyIdOnEveryMigratedRow()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(store);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.Products.SingleAsync()).LegacyProductId.Should().Be(4242);
        (await db.Invoices.SingleAsync()).LegacyInvoiceId.Should().Be(7001);
        (await db.InvoiceLines.SingleAsync()).LegacyInvoiceLineId.Should().Be(8001);
        (await db.Payments.SingleAsync()).LegacyPaymentId.Should().Be(9001);
    }

    [Fact]
    public async Task MigrateAll_TwoStoresSharingALegacyId_BothMigrateIntoOneDatabase()
    {
        // The reason uniqueness is scoped per store rather than global. Legacy id ranges OVERLAP
        // across the real stores, so a globally unique index would reject the second store on a
        // shared database. StoreUser.LegacyUserId has exactly that global index today -- harmless
        // only because each store currently gets its own database, and deliberately left alone here
        // rather than widened as a side effect of this change.
        await using var first = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(first);
        await RunForStoreAsync(first, "STORE-A");

        await using var second = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(second);
        await RunForStoreAsync(second, "STORE-B");

        await using var db = _postgres.CreateDbContext();

        var products = await db.Products.Where(p => p.LegacyProductId == 4242).ToListAsync();
        products.Should().HaveCount(2, "the same legacy id in two stores is not a duplicate");
        products.Select(p => p.StoreId).Should().BeEquivalentTo(["STORE-A", "STORE-B"]);

        (await db.Invoices.CountAsync(i => i.LegacyInvoiceId == 7001)).Should().Be(2);
    }

    [Fact]
    public async Task MigrateInvoiceLines_ASynthesisedPlaceholder_HasNoLegacyProductId()
    {
        // A placeholder is invented by defect 13 for a line whose product was deleted, so there is no
        // legacy product to record. Null, not the line's own id: writing that would claim the product
        // row came from a legacy product row that does not exist.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(7001, userId: 1, total: 80m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 8001, invoiceId: 7001, productId: 777, barcode: "8850009999999",
            description: "Product deleted years ago", quantity: 1, unitPrice: 80m,
            originalUnitPrice: 80m);
        await builder.AddPaymentAsync(
            paymentId: 9001, invoiceId: 7001, paymentTypeId: 1, amount: 80m,
            dateCreated: "2024-03-15 14:30:00");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var placeholder = await db.Products.SingleAsync();

        placeholder.IsActive.Should().BeFalse("sanity: this is the synthesised placeholder");
        placeholder.LegacyProductId.Should().BeNull("it was invented, not migrated from a legacy row");

        (await db.InvoiceLines.SingleAsync()).LegacyInvoiceLineId.Should().Be(8001,
            "the LINE is real and keeps its id even when its product had to be invented");
    }

    private async Task RunForStoreAsync(LegacyStoreDatabase store, string storeId)
    {
        var options = new MigrationOptions
        {
            SqlitePath = store.Path,
            PostgresConnectionString = _postgres.ConnectionString,
            StoreId = storeId,
            DryRun = false
        };

        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();
    }
}
