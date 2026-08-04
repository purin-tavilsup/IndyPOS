using Dapper;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Defect 12. A phase-level throw used to escape MigrateAllAsync entirely, and because
/// SaveChangesAsync runs after the last phase, an entire in-memory migration was discarded with only
/// a stack trace to show for it.
///
/// The drift is induced the way it will really happen -- a real artefact schema with a column
/// removed -- because that is exactly what defects 2 and 3 were.
/// </summary>
[Collection("Postgres")]
public class MigrationPhaseIsolationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public MigrationPhaseIsolationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// A healthy credit sale: user, product, invoice, line, type-2 payment, PayLater extension.
    /// Every phase has something to do, so "an earlier phase succeeded" is a real claim.
    /// </summary>
    private static async Task SeedOneOfEverythingAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 700m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 700m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 1, unitPrice: 700m, originalUnitPrice: 700m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 2, amount: 700m,
            dateCreated: "2024-03-15 14:30:00", note: "Somchai");
        await builder.AddPayLaterAsync(
            paymentId: 500, invoiceId: 1, description: "Somchai", payLaterAmount: 700m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00");
    }

    /// <summary>
    /// Drops a column the migrator's SELECT names by hand, which is what column drift looks like.
    /// PaidAmount is a plain NUMERIC with no index, no UNIQUE and no PK role, so SQLite permits it.
    /// </summary>
    private static Task DropPayLaterPaidAmountAsync(LegacyStoreDatabase store) =>
        store.Connection.ExecuteAsync("ALTER TABLE PayLater DROP COLUMN PaidAmount;");

    [Fact]
    public async Task MigrateAllAsync_WhenAPhaseFails_DoesNotThrowAndNamesThePhase()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await DropPayLaterPaidAmountAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PhaseFailures.Should().ContainSingle(
            "the PayLater SELECT names PaidAmount, so it throws -- and that must be caught");
        var failure = result.PhaseFailures.Single();
        failure.Phase.Should().Be("PayLater");
        failure.Message.Should().Contain("PaidAmount",
            "the operator needs the actual cause, not just the phase name");
    }

    [Fact]
    public async Task MigrateAllAsync_WhenAPhaseFails_ReportsFailureSoTheExitCodeIsNonZero()
    {
        // Program.cs:163 is `context.ExitCode = result.IsSuccess ? 0 : 1`. Before this fix the phase
        // threw and was caught there, so the operator got exit 1. Now it returns normally -- so
        // IsSuccess is the ONLY thing standing between a store that migrated nothing and a green
        // "success" on the console.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await DropPayLaterPaidAmountAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(MigrationOutcome.Aborted);
    }

    [Fact]
    public async Task MigrateAllAsync_WhenAPhaseFails_PersistsNothingFromEarlierPhases()
    {
        // The property that keeps defect 12 merely expensive rather than catastrophic. Users,
        // products and invoices all migrated successfully IN MEMORY before PayLater threw. None of
        // it may reach PostgreSQL: a store holding products and part of its sales history, reported
        // as a success, is worse than no migration at all.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await DropPayLaterPaidAmountAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.Users.Migrated.Should().Be(1, "the Users phase itself succeeded");
        result.Products.Migrated.Should().Be(1);
        result.Invoices.Migrated.Should().Be(1);

        await using var db = _postgres.CreateDbContext();
        (await db.StoreUsers.CountAsync()).Should().Be(0, "no phase may be committed when another failed");
        (await db.Products.CountAsync()).Should().Be(0);
        (await db.Invoices.CountAsync()).Should().Be(0);
        (await db.Payments.CountAsync()).Should().Be(0);
        (await db.PayLaters.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task MigrateAllAsync_WhenAnEarlyPhaseFails_StillAttemptsTheLaterOnes()
    {
        // The whole point of isolating the diagnosis: one attempt reports every schema problem.
        // Re-running against a real shop costs a visit with the till switched off, so learning about
        // one broken phase per run is expensive.
        // Manufacturer is named explicitly in the Products SELECT and is a plain nullable TEXT with
        // no index, so dropping it makes that phase -- and only that phase -- throw.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneOfEverythingAsync(store);
        await store.Connection.ExecuteAsync("ALTER TABLE InventoryProduct DROP COLUMN Manufacturer;");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PhaseFailures.Should().ContainSingle().Which.Phase.Should().Be("Products");
        result.Users.Migrated.Should().Be(1, "the phase before the failure still ran");
        result.Invoices.Migrated.Should().Be(1, "the phase AFTER the failure still ran");
        result.PayLater.Migrated.Should().Be(1, "and so did the last one");
        result.IsSuccess.Should().BeFalse("a phase still failed, so nothing was written");
    }

    [Fact]
    public async Task MigrateAllAsync_WithMoreRowErrorsThanTheCap_BoundsTheStringsButNotTheCounts()
    {
        // Drives the cap through the real service. This run has NO phase failure -- it reaches the cap
        // with ordinary per-row refusals, which is the cheapest way to exercise it. The cascade the cap
        // exists for (a failed Products phase making every invoice line miss its lookup, up to 325,780
        // near-identical strings on real data) is the same code path with a bigger multiplier.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 1m, quantityInStock: 5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        // 150 invoices, each paid by a legacy type with no catalogue code, so each records one
        // per-row error in the Invoices phase -- 150 > the cap of 100.
        for (var i = 1; i <= 150; i++)
        {
            await builder.AddInvoiceAsync(i, userId: 1, total: 1m, dateCreated: "2024-03-15 14:30:00");
            await builder.AddPaymentAsync(
                paymentId: 500 + i, invoiceId: i, paymentTypeId: 6, amount: 1m,
                dateCreated: "2024-03-15 14:30:00");
        }

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.Payments.Failed.Should().Be(150, "the COUNT must stay exact");
        result.Errors.Should().HaveCount(101, "100 strings plus one suppression note");
        result.Errors.Last().Should().Be("Invoices: 50 further error(s) suppressed.");
    }
}
