using Dapper;
using FluentAssertions;
using IndyPOS.MigrationTool;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Defect 20: a payment whose invoice does not exist was dropped in silence.
/// </summary>
/// <remarks>
/// <c>MigrateInvoicesAsync</c> iterates invoices and attaches each one's payments, so a
/// <c>Payment</c> row pointing at an <c>InvoiceId</c> absent from <c>Invoice</c> is never visited by
/// any code path. Real GeneralHardware data holds two, ฿1,000 of cash, and the run reported
/// <c>Errors: 0</c>. <c>Total Revenue</c> cannot catch it, because that sums <c>Invoice.Total</c> and
/// those invoices do not exist.
/// <para>
/// It is refused rather than rescued, matching how PayLater already treats a row whose parent is
/// missing: there is no invoice to attach the money to, and inventing one would fabricate a sale
/// nobody made.
/// </para>
/// </remarks>
[Collection("Postgres")]
public class OrphanPaymentMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private LegacyStoreDatabase _store = null!;

    public OrphanPaymentMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await _postgres.ResetDatabaseAsync();
    }

    public async Task DisposeAsync() => await _store.DisposeAsync();

    /// <summary>
    /// One good invoice with its payment, plus a payment whose invoice was deleted — the exact shape
    /// of GeneralHardware's PaymentId 78, which points at the absent InvoiceId 77.
    /// </summary>
    private async Task SeedOneGoodAndOneOrphanedPaymentAsync()
    {
        var builder = new LegacyStoreDataBuilder(_store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 1, barcode: "8850001000001", description: "Cement",
            unitPrice: 120m, quantityInStock: 50, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        await builder.AddInvoiceAsync(1, userId: 1, total: 120m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 1, barcode: "8850001000001",
            description: "Cement", quantity: 1, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddPaymentAsync(
            paymentId: 1, invoiceId: 1, paymentTypeId: 1, amount: 120m,
            dateCreated: "2024-03-15 14:30:00");

        // Invoice 77 is never inserted.
        await builder.AddPaymentAsync(
            paymentId: 78, invoiceId: 77, paymentTypeId: 1, amount: 500m,
            dateCreated: "2024-03-15 15:00:00");
    }

    [Fact]
    public async Task MigrateInvoices_WithAPaymentWhoseInvoiceIsMissing_ReportsItInsteadOfDroppingIt()
    {
        await SeedOneGoodAndOneOrphanedPaymentAsync();

        var result = await MigrationScenario.RunAsync(_store, _postgres);

        result.Errors.Should().Contain(
            e => e.Contains("78") && e.Contains("77") && e.Contains("500"),
            "the operator needs the payment, the invoice it points at, and the amount at stake");
        result.Payments.Failed.Should().Be(1, "the money could not be migrated, so it is not a success");
        result.Payments.Migrated.Should().Be(1, "the good payment is unaffected");
    }

    [Fact]
    public async Task MigrateInvoices_WithAPaymentWhoseInvoiceIsMissing_StillMigratesEverythingElse()
    {
        // Refusing one orphan must not cost the store its real history.
        await SeedOneGoodAndOneOrphanedPaymentAsync();

        await MigrationScenario.RunAsync(_store, _postgres);

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(1);
        (await db.InvoiceLines.CountAsync()).Should().Be(1);
        (await db.Payments.CountAsync()).Should().Be(1, "only the attachable payment is written");
        (await db.Payments.SingleAsync()).Amount.Should().Be(120m);
    }

    [Fact]
    public async Task VerifyAsync_WithAnOrphanedPayment_ReportsItAsItsOwnRowNotAsAMethodMismatch()
    {
        // Without this the per-method check blames the migration for a source-data inconsistency:
        // GeneralHardware read "Cash 121,669 vs 121,667" with no indication why. The expectation now
        // excludes payments that cannot be attached, and the orphans get a row of their own.
        await SeedOneGoodAndOneOrphanedPaymentAsync();

        var options = new MigrationOptions
        {
            SqlitePath = _store.Path,
            PostgresConnectionString = _postgres.ConnectionString,
            StoreId = MigrationScenario.StoreId,
            DryRun = false
        };

        await new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance)
            .MigrateAllAsync();

        var result = await new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance)
            .VerifyAsync();

        result.Checks.Should().Contain(c => c.EntityName == "Payments (no invoice)",
            "the orphans are named, not folded into another check");
        result.Checks.First(c => c.EntityName == "Payments (no invoice)").SqliteCount.Should().Be(1);

        result.Checks.First(c => c.EntityName == "Payments [Cash]").IsValid.Should().BeTrue(
            "an unattachable payment is not a Cash-method mismatch");
        result.Checks.First(c => c.EntityName == "Payments").IsValid.Should().BeTrue(
            "nor a bare count shortfall the operator cannot act on");
    }
}
