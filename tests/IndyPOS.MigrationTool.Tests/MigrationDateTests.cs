using System.Globalization;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Defects 4 and 11 are FIXED; both tests below were pins that have now been inverted. They assert
/// the correct answer: a legacy timestamp is Thai local time, and it must parse under any culture.
/// </summary>
[Collection("Postgres")]
public class MigrationDateTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;

    public MigrationDateTests(PostgresFixture postgres) => _postgres = postgres;

    public Task InitializeAsync() => _postgres.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>Thai local time, the only kind a legacy store records.</summary>
    private const string ThaiLocalTimestamp = "2024-03-15 14:30:00";

    private static async Task SeedOneInvoiceAsync(LegacyStoreDatabase store)
    {
        var builder = new LegacyStoreDataBuilder(store);
        await builder.AddPaymentTypeLookupAsync();
        await builder.AddUserAsync(1, "cashier", "Somchai", "Jaidee", 1, ThaiLocalTimestamp);
        await builder.AddInvoiceAsync(1, userId: 1, total: 120m, dateCreated: ThaiLocalTimestamp);
        await builder.AddPaymentAsync(
            paymentId: 500, invoiceId: 1, paymentTypeId: 1, amount: 120m,
            dateCreated: ThaiLocalTimestamp);
    }

    /// <summary>The same instant expressed as UTC. Thailand is UTC+7 with no DST, ever.</summary>
    private static readonly DateTime ExpectedUtc = new(2024, 3, 15, 7, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task MigrateInvoices_DateCreated_ConvertsThaiLocalToUtc_Defect4()
    {
        // Defect 4 (FIXED): ParseDate used to SpecifyKind(..., Utc) on a value written by
        // datetime('now','localtime'), so a 14:30 Bangkok sale became 14:30 UTC.
        // Across 139,680 real invoices every timestamp was 7 hours out, which silently corrupted
        // every daily and monthly total while reconciling perfectly under any count-based check.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
            await SeedOneInvoiceAsync(store);

            await MigrationScenario.RunAsync(store, _postgres);

            await using var db = _postgres.CreateDbContext();
            var invoice = await db.Invoices.SingleAsync();

            invoice.CreatedUtc.Should().Be(ExpectedUtc);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task MigrateInvoices_UnderThaiCulture_ParsesTheGregorianYear_Defect11()
    {
        // Defect 11 (FIXED): ParseDate called bare DateTime.TryParse with NO CultureInfo, and the
        // migration tool runs on the store's own Thai-locale till. Under th-TH the Buddhist calendar
        // read 2024 as a Buddhist-era year, giving 1481 AD -- 543 years off. Every invoice then fell
        // outside every date-range report, so a migrated store showed ZERO sales history.
        // Same expected value as defect 4's test: the ONLY difference is the ambient culture, which
        // is exactly what must stop mattering.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
        try
        {
            await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
            await SeedOneInvoiceAsync(store);

            await MigrationScenario.RunAsync(store, _postgres);

            await using var db = _postgres.CreateDbContext();
            var invoice = await db.Invoices.SingleAsync();

            invoice.CreatedUtc.Should().Be(ExpectedUtc);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
