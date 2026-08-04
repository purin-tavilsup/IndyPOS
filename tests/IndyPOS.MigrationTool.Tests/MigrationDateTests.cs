using System.Globalization;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.Tools;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// PINNING TESTS. These assert what the migrator does TODAY, including its defects, so the suite
/// stays green and meaningful while the fixes land in their own specs. Each names its defect and
/// records the correct answer. When a defect is fixed, invert exactly one test here.
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

    [Fact]
    public async Task MigrateInvoices_DateCreated_CurrentlyRelabelsThaiLocalAsUtc_Defect4()
    {
        // Defect 4: ParseDate does SpecifyKind(..., Utc) on a value written by
        // datetime('now','localtime'), so a 14:30 Bangkok sale becomes 14:30 UTC.
        // CORRECT: 2024-03-15T07:30:00Z (Thailand is UTC+7).
        // Across 139,680 real invoices every timestamp is 7 hours out, which silently corrupts
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

            invoice.CreatedUtc.Should().Be(new DateTime(2024, 3, 15, 14, 30, 0, DateTimeKind.Utc));
            invoice.CreatedUtc.Should().NotBe(new DateTime(2024, 3, 15, 7, 30, 0, DateTimeKind.Utc),
                "this is the correct answer and defect 4 does not yet produce it");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public async Task MigrateInvoices_UnderThaiCulture_CurrentlyLandsIn1481_Defect11()
    {
        // Defect 11: ParseDate calls bare DateTime.TryParse with NO CultureInfo, and the migration
        // tool runs on the store's own Thai-locale till. Under th-TH the Buddhist calendar reads
        // 2024 as a Buddhist-era year, giving 1481 AD -- 543 years off.
        // CORRECT: year 2024, by parsing with CultureInfo.InvariantCulture.
        // Every invoice then falls outside every date-range report, so a migrated store shows ZERO
        // sales history. Same six-line method as defect 4.
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("th-TH");
        try
        {
            await using var store = await LegacyStoreDatabase.CreateAsync(LegacyStoreShape.GeneralHardware);
            await SeedOneInvoiceAsync(store);

            await MigrationScenario.RunAsync(store, _postgres);

            await using var db = _postgres.CreateDbContext();
            var invoice = await db.Invoices.SingleAsync();

            invoice.CreatedUtc.Year.Should().Be(1481,
                "the Thai Buddhist calendar reads 2024 as a BE year; 2024 - 543 = 1481");
            invoice.CreatedUtc.Year.Should().NotBe(2024,
                "this is the correct answer and defect 11 does not yet produce it");
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
