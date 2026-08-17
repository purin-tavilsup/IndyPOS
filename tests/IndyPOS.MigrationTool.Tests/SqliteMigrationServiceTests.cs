using Dapper;
using IndyPOS.MigrationTool;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class SqliteMigrationServiceTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public SqliteMigrationServiceTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task SeedOneSaleAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");
        await builder.AddInvoiceAsync(1, userId: 1, total: 120m, dateCreated: "2024-03-15 14:30:00");
        await builder.AddInvoiceLineAsync(
            invoiceProductId: 1, invoiceId: 1, productId: 10, barcode: "8850001000010",
            description: "Cement 50kg", quantity: 1, unitPrice: 120m, originalUnitPrice: 120m);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 120m,
            dateCreated: "2024-03-15 14:30:00");
    }

    [Fact]
    public async Task MigrateAllAsync_WithAnEmptyStore_ShouldSucceedAndMigrateNothing()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.TotalMigrated.Should().Be(0);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task MigrateAllAsync_CalledTwice_RefusesTheSecondRunAndWritesNothing()
    {
        // CHANGED BEHAVIOUR, and an improvement. This test used to assert the second run SUCCEEDED,
        // with users and products skipped -- while saying nothing about invoices, which it silently
        // DUPLICATED. Measured on real data before this change: a second run took MimyShop from 15
        // invoices and THB 1,056 to 30 and THB 2,112, doubling recorded turnover.
        //
        // Defect 8's per-store unique index on the legacy invoice id makes that duplication
        // impossible, so a re-run can no longer quietly double a store's history. Left to the
        // database it surfaced as an opaque 23505 escaping MigrateAllAsync, so the run is now refused
        // up front with a message that says what happened and that nothing was written.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(store);

        var first = await MigrationScenario.RunAsync(store, _postgres);
        var second = await MigrationScenario.RunAsync(store, _postgres);

        first.IsSuccess.Should().BeTrue();

        second.IsSuccess.Should().BeFalse("re-running would duplicate this store's sales history");
        second.Outcome.Should().Be(MigrationOutcome.Aborted, "nothing may be written");
        second.PhaseFailures.Should().ContainSingle()
              .Which.Message.Should().Contain("already").And.Contain("1");

        await using var db = _postgres.CreateDbContext();
        (await db.StoreUsers.CountAsync()).Should().Be(1, "users must not be duplicated");
        (await db.Products.CountAsync()).Should().Be(1, "products must not be duplicated");
        (await db.Invoices.CountAsync()).Should().Be(1, "and neither must invoices");
        (await db.Payments.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task MigrateAllAsync_AgainstADifferentStoreInTheSameDatabase_IsNotRefused()
    {
        // The guard must be per store, not per database. Two stores legitimately share one database
        // in a shared-hosting setup, and their legacy ids overlap.
        await using var first = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(first);
        await MigrationScenario.RunAsync(first, _postgres);

        await using var second = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(second);

        var options = new MigrationOptions
        {
            SqlitePath = second.Path,
            PostgresConnectionString = _postgres.ConnectionString,
            StoreId = "A-DIFFERENT-STORE",
            DryRun = false
        };

        var result = await new SqliteMigrationService(
            options, NullLogger<SqliteMigrationService>.Instance).MigrateAllAsync();

        result.IsSuccess.Should().BeTrue("a different store has migrated nothing yet");

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(2, "one invoice per store");
    }

    [Fact]
    public async Task MigrateAllAsync_WithAUserThatHasNoCredentials_ShouldSkipThatUser()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await store.Connection.ExecuteAsync("""
            INSERT INTO User (UserId, FirstName, LastName, RoleId, DateCreated)
            VALUES (9, 'No', 'Credentials', 1, '2024-03-15 09:00:00');
            """);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Users.Skipped.Should().Be(1);
        result.Users.Migrated.Should().Be(0);
    }

    [Fact]
    public async Task MigrateAllAsync_ShouldIgnoreTheObsoleteCustomersAndInstallmentsTables()
    {
        // Customers and Installments are obsolete and never used in any store (0 rows measured;
        // confirmed by Pond 2026-08-03). Legacy payment type 6 (ผ่อนชำระ) is dead with them.
        //
        // They exist in the GeneralHardware schema, so this pins that the migration ignores them --
        // otherwise someone later "completes" the migration by adding them, importing a feature no
        // store uses and giving legacy type 6 a home it should not have.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(store);

        await store.Connection.ExecuteAsync("""
            INSERT INTO Customers (CustomerId, FirstName, LastName, DateCreated)
            VALUES (1, 'Obsolete', 'Feature', '2024-03-15 09:00:00');
            INSERT INTO Installments (CustomerId, Installment, NumberOfInstallments, Total, DateCreated, DueDate)
            VALUES (1, 'never used', 3, '900', '2024-03-15 09:00:00', '2024-06-15');
            """);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty("obsolete tables must be ignored, not reported as a problem");

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync()).Should().Be(1);
        (await db.PayLaters.CountAsync()).Should().Be(0,
            "an Installments row must never become a PayLater");
    }

    [Fact]
    public async Task MigrateAllAsync_InDryRun_ShouldWriteNothing()
    {
        // The payments loop runs outside the DryRun guard by design (defect 10 fix), so that
        // MigrationResult.PaymentIdMap is always built and PayLater can resolve against it. Only
        // the context.Payments.Add call stays guarded. These assertions pin that invariant: a
        // dry run still counts the payment as migrated, but writes nothing.
        //
        // The negative-stock clamp (defect 14b) is recorded outside the guard for the same class of
        // reason: a dry run exists to preview what a real run would do, and clamping stock is the
        // one thing it does that the operator must decide about beforehand. Pinned here so a later
        // tidy-up cannot quietly move the recording inside the guard.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedOneSaleAsync(store);
        await new LegacyStoreDataBuilder(store).AddProductAsync(
            productId: 11, barcode: "8850001000027", description: "Sand 25kg",
            unitPrice: 80m, quantityInStock: -5, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        var result = await MigrationScenario.RunAsync(store, _postgres, dryRun: true);

        result.Users.Migrated.Should().Be(1);
        result.Products.Migrated.Should().Be(2);
        result.PaymentIdMap.Should().ContainKey(500,
            "the map must be built in dry run too, or every PayLater lookup would fail");
        result.ClampedStocks.Should().ContainSingle(
            "a dry run must still report the clamp, or the preview hides the data change")
            .Which.LegacyQuantity.Should().Be(-5);

        await using var db = _postgres.CreateDbContext();
        (await db.StoreUsers.CountAsync()).Should().Be(0);
        (await db.Products.CountAsync()).Should().Be(0);
        (await db.Payments.CountAsync()).Should().Be(0, "dry run must not write payments either");
        (await db.Invoices.CountAsync()).Should().Be(0);
        (await db.InvoiceLines.CountAsync()).Should().Be(0);
        (await db.InventoryMovements.CountAsync()).Should().Be(0);
    }
}
