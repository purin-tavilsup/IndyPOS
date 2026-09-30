using FluentAssertions;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.MigrationTool;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Every imported v3 invoice keeps its v3 number as its bill number, the sequence carries
/// on after the highest one, and a target that already took v4 sales is refused.
/// </summary>
[Collection("Postgres")]
public class InvoiceNumberMigrationTests : IAsyncLifetime
{
    private const long LowerLegacyNumber = 6999;
    private const long HigherLegacyNumber = 7001;
    private const string OtherStoreId = "ANOTHER-STORE";

    private readonly PostgresFixture _postgres;

    public InvoiceNumberMigrationTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Two sales, the HIGHER legacy id seeded first, so "the last row" is not the maximum.</summary>
    private static async Task SeedTwoSalesAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, "2024-03-15 09:00:00");
        await builder.AddProductAsync(
            productId: 10, barcode: "8850001000010", description: "Cement 50kg",
            unitPrice: 120m, quantityInStock: 20, category: 50, isTrackable: true,
            dateCreated: "2024-03-15 09:00:00");

        foreach (var (invoiceId, paymentId) in new[] { ((int)HigherLegacyNumber, 9001), ((int)LowerLegacyNumber, 9002) })
        {
            await builder.AddInvoiceAsync(invoiceId, userId: 1, total: 120m, dateCreated: "2024-03-15 14:30:00");
            await builder.AddInvoiceLineAsync(
                invoiceProductId: invoiceId, invoiceId: invoiceId, productId: 10, barcode: "8850001000010",
                description: "Cement 50kg", quantity: 1, unitPrice: 120m, originalUnitPrice: 120m);
            await builder.AddPaymentAsync(
                paymentId: paymentId, invoiceId: invoiceId, paymentTypeId: 1, amount: 120m,
                dateCreated: "2024-03-15 14:30:00");
        }
    }

    /// <summary>An invoice v4 created itself: no legacy id, number from the column default.</summary>
    private async Task<Invoice> SeedNativeInvoiceAsync(string storeId)
    {
        await using var db = _postgres.CreateDbContext();
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            UserId = Guid.NewGuid(),
            TotalAmount = 50m,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow
        };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice;
    }

    private MigrationOptions Options(LegacyStoreDatabase store) => new()
    {
        SqlitePath = store.Path,
        PostgresConnectionString = _postgres.ConnectionString,
        StoreId = MigrationScenario.StoreId,
        DryRun = false
    };

    [Fact]
    public async Task MigrateAll_WhenTheTargetStoreHasANativeInvoice_RefusesTheRun()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await SeedNativeInvoiceAsync(MigrationScenario.StoreId);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.PhaseFailures.Should()
                            .ContainSingle(f => f.Phase == "NativeInvoices");
    }

    [Fact]
    public async Task MigrateAll_WhenTheTargetStoreHasANativeInvoice_WritesNoLegacyInvoice()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await SeedNativeInvoiceAsync(MigrationScenario.StoreId);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.CountAsync(i => i.LegacyInvoiceId != null)).Should()
                                                                      .Be(0);
    }

    [Fact]
    public async Task MigrateAll_WhenOnlyAnotherStoreHasANativeInvoice_Succeeds()
    {
        // Bill numbers are unique per store, so another store's v4 sales cannot collide.
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await SeedNativeInvoiceAsync(OtherStoreId);

        var result = await MigrationScenario.RunAsync(store, _postgres);

        result.IsSuccess.Should()
                        .BeTrue();
    }

    [Fact]
    public async Task MigrateAll_WithALegacyInvoice_WritesItsV3NumberAsTheBillNumber()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);

        await MigrationScenario.RunAsync(store, _postgres);

        await using var db = _postgres.CreateDbContext();
        (await db.Invoices.SingleAsync(i => i.LegacyInvoiceId == HigherLegacyNumber)).InvoiceNumber.Should()
                                                                                              .Be(HigherLegacyNumber);
    }

    [Fact]
    public async Task MigrateAll_AfterImport_GivesTheNextSaleTheNumberAfterTheHighestV3Number()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        await MigrationScenario.RunAsync(store, _postgres);

        var nextSale = await SeedNativeInvoiceAsync(MigrationScenario.StoreId);

        nextSale.InvoiceNumber.Should()
                              .Be(HigherLegacyNumber + 1);
    }

    [Fact]
    public async Task BuildBulkMigrationRequest_AfterImport_CarriesTheV3BillNumber()
    {
        await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
        await SeedTwoSalesAsync(store);
        var service = new SqliteMigrationService(Options(store), NullLogger<SqliteMigrationService>.Instance);
        await service.MigrateAllAsync();

        var request = await service.BuildBulkMigrationRequestAsync(CancellationToken.None);

        request.Invoices.Select(i => i.InvoiceNumber).Should()
                                                     .BeEquivalentTo(new long?[] { HigherLegacyNumber, LowerLegacyNumber });
    }
}
