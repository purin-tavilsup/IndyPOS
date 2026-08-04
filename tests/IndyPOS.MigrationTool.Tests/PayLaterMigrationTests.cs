using IndyPOS.Application.Common.Constants;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Runs the shipped migrator against a legacy schema taken from a real store.
/// </summary>
public static class MigrationScenario
{
    public const string StoreId = "TEST-STORE";

    public static async Task<MigrationResult> RunAsync(
        LegacyStoreDatabase store, PostgresFixture postgres, bool dryRun = false)
    {
        var options = new MigrationOptions
        {
            SqlitePath = store.Path,
            PostgresConnectionString = postgres.ConnectionString,
            StoreId = StoreId,
            DryRun = dryRun
        };

        return await new SqliteMigrationService(
            options, NullLogger<SqliteMigrationService>.Instance).MigrateAllAsync();
    }
}

[Collection("Postgres")]
public class PayLaterMigrationTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public PayLaterMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Seeds one credit sale: an invoice, a line, a PaymentTypeId=2 payment, and the PayLater
    /// extension row keyed by that payment's id -- the shape a real till writes.
    /// </summary>
    private static async Task SeedCreditSaleAsync(
        LegacyStoreDatabase store,
        decimal payLaterAmount = 700m,
        decimal paidAmount = 299m,
        bool isCompleted = false,
        string customer = "Somchai (shop next door)")
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 700m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: payLaterAmount, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 1, unitPrice: payLaterAmount,
            originalUnitPrice: payLaterAmount);

        // The till writes the Payment FIRST, then the PayLater that tracks the debt.
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 2, amount: payLaterAmount,
            dateCreated: "2024-03-15 14:30:00", note: customer);
        await builder.AddPayLaterAsync(
            paymentId: 500, invoiceId: 1, description: customer, payLaterAmount: payLaterAmount,
            paidAmount: paidAmount, isCompleted: isCompleted, dateCreated: "2024-03-15 14:30:00",
            dateUpdated: "2024-04-02 11:05:00");
    }

    [Fact]
    public async Task MigrateAllAsync_AgainstGeneralHardwareShape_CompletesAndPersists()
    {
        // Defect 2. Before the fix this THROWS "no such column: PayLaterId", and because that
        // query sits outside the per-row try and SaveChangesAsync runs after the PayLater phase,
        // the entire migration is discarded -- nothing is persisted, for any real store.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(1, "the run must reach SaveChangesAsync");
        (await db.Products.CountAsync()).Should().Be(1);
        (await db.StoreUsers.CountAsync()).Should().Be(1);
        (await db.PayLaters.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MigrateAllAsync_AgainstAStoreWithNoPayLaterTable_Completes()
    {
        // Defect 3. The PayLater query runs unconditionally, so on MimyShop and MimyMart -- which
        // have no such table -- it throws "no such table: PayLater". PayLater is a
        // GeneralHardware-only feature, so its absence is normal, not a failure.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.MimyShop);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Malee", "Sooksan", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850002000020", description: "Instant noodles",
            unitPrice: 6m, quantityInStock: 100, category: 20, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 6m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850002000020",
            description: "Instant noodles", quantity: 1, unitPrice: 6m, originalUnitPrice: 6m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 6m,
            dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.PayLater.Migrated.Should().Be(0);
        result.PayLater.Failed.Should().Be(0, "a store with no PayLater feature is not a failure");
        result.Errors.Should().BeEmpty();

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MigratePayLater_ReadsRealColumns_MapsDescriptionAndAmounts()
    {
        // Defect 2. PaidAmount is READ, never derived from IsCompleted: the customer returns and
        // pays in instalments, so PaidAmount is the only surviving record of that progress.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 700m, paidAmount: 299m, isCompleted: false);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.Description.Should().Be("Somchai (shop next door)");
        payLater.PayLaterAmount.Should().Be(700m);
        payLater.PaidAmount.Should().Be(299m, "read from the column, not derived from IsCompleted");
        payLater.IsCompleted.Should().BeFalse();
        payLater.RemainingAmount.Should().Be(401m);
    }

    [Fact]
    public async Task MigratePayLater_LinksTheExistingPayment_WithoutCreatingASecond()
    {
        // Defect 10. At the till a credit sale writes the Payment first, then the PayLater that
        // tracks the debt. MigratePayLaterAsync re-enacted that sequence -- correct for a NEW sale,
        // wrong for a migration, because both rows already exist in SQLite. On real GeneralHardware
        // data that double-counts THB 836,013 across 5,181 rows.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 700m, paidAmount: 299m);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLaterPayments = await db.Payments
            .Where(p => p.Method == PaymentMethodCodes.PayLater)
            .ToListAsync();

        payLaterPayments.Should().HaveCount(1, "the legacy Payment row is the money; PayLater only annotates it");
        payLaterPayments.Single().Amount.Should().Be(700m);
        (await db.Payments.SumAsync(p => p.Amount)).Should().Be(700m, "no money may be invented");

        var payLater = await db.PayLaters.SingleAsync();
        payLater.PaymentId.Should().Be(payLaterPayments.Single().Id,
            "the extension row must point at the migrated payment");
    }

    [Fact]
    public async Task MigratePayLater_WhenThePaymentIsMissing_RecordsAnErrorAndSkips()
    {
        // The real ฿70 orphan is the reverse case: PaymentId 90 is a type-2 Payment with an empty
        // Note and no PayLater row. Here we test the other direction -- a PayLater whose payment
        // was never migrated must be refused, never furnished with an invented payment.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 700m, dateCreated: "2024-03-15 14:30:00");
        // No Payment row at all, so PaymentId 999 cannot resolve.
        await builder.AddPayLaterAsync(
            paymentId: 999, invoiceId: 1, description: "Somchai", payLaterAmount: 700m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00");

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PayLater.Failed.Should().Be(1);
        result.Errors.Should().ContainSingle().Which.Should().Contain("999");

        await using var db = _postgres.CreateDbContext();
        (await db.PayLaters.CountAsync()).Should().Be(0);
        (await db.Payments.CountAsync()).Should().Be(0, "a missing payment must never be invented");
    }

    [Fact]
    public async Task MigratePayLater_WithPaidAmountExceedingTheDebt_CarriesItUnchanged()
    {
        // Real data holds PaymentId 139978: THB 82 owed against THB 8,200 recorded paid -- exactly
        // 100x, a dropped decimal at the till, with IsCompleted still 0 because the app's
        // completion check compares for equality. The migration must NOT normalise or clamp this.
        // Inventing a correction is worse than carrying a visible error: a store can only fix what
        // it can see.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 82m, paidAmount: 8200m, isCompleted: false);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.PayLaterAmount.Should().Be(82m);
        payLater.PaidAmount.Should().Be(8200m, "carried verbatim; the migration does not invent corrections");
        payLater.IsCompleted.Should().BeFalse();
        payLater.RemainingAmount.Should().Be(-8118m, "a negative remainder is the visible symptom");
    }

    [Fact]
    public async Task MigratePayLater_WithNeverRepaidRow_PreservesZeroPaidAmount()
    {
        // The 142-row case: nothing repaid yet, DateUpdated null.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 500m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 2, amount: 500m,
            dateCreated: "2024-03-15 14:30:00", note: "Malee");
        await builder.AddPayLaterAsync(
            paymentId: 500, invoiceId: 1, description: "Malee", payLaterAmount: 500m,
            paidAmount: 0m, isCompleted: false, dateCreated: "2024-03-15 14:30:00",
            dateUpdated: null);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.PaidAmount.Should().Be(0m);
        payLater.IsCompleted.Should().BeFalse();
        payLater.RemainingAmount.Should().Be(500m);
        // Asserted as a CONCRETE value, not just == CreatedUtc: the service computes createdUtc once
        // and uses it as the fallback, so if ParseDate returned null for BOTH columns the two would
        // still be equal and the assertion would prove nothing about DateUpdated.
        payLater.LastModifiedUtc.Should().Be(new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc),
            "a null DateUpdated falls back to the parsed created timestamp");
        payLater.LastModifiedUtc.Should().Be(payLater.CreatedUtc);
    }

    [Fact]
    public async Task MigratePayLater_CustomerName_SurvivesInBothDescriptionAndNote()
    {
        // Measured: PayLater.Description and the linked Payment.Note hold the same customer name on
        // all 5,181 real rows -- zero disagreements, zero rows where only one is set. Description is
        // the intended home; Note mirrors it. Both migrate through their own columns.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, customer: "คุณสมชาย ใจดี");

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();

        (await db.PayLaters.SingleAsync()).Description.Should().Be("คุณสมชาย ใจดี");
        (await db.Payments.SingleAsync()).Note.Should().Be("คุณสมชาย ใจดี");
    }

    [Fact]
    public async Task MigratePayLater_WhenCompleted_PreservesFullPayment()
    {
        // The 5,037-row case: settled in full, IsCompleted 1.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedCreditSaleAsync(store, payLaterAmount: 195m, paidAmount: 195m, isCompleted: true);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        var payLater = await db.PayLaters.SingleAsync();

        payLater.IsCompleted.Should().BeTrue();
        payLater.PaidAmount.Should().Be(195m);
        payLater.RemainingAmount.Should().Be(0m);
    }
}
