using FluentAssertions;
using IndyPOS.Domain.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Migrations;

/// <summary>
/// Exercises the ReclassifyPaymentMethodKinds data migration against a real PostgreSQL
/// database that already holds catalog rows.
/// <para>
/// This is the only coverage of that migration's row-flipping branch: IntegrationTestBase
/// provisions with EnsureCreatedAsync (no migrations at all), and a fresh install runs the
/// migration before the seeder, so its UPDATEs hit an empty table. The three live stores are
/// the only place the branch actually does work — hence this test.
/// </para>
/// </summary>
public class ReclassifyPaymentMethodKindsMigrationTests
    : IClassFixture<ReclassifyPaymentMethodKindsMigrationTests.MigratedCatalogFixture>
{
    private const string LegacyStore = "store-legacy";
    private const string HandEditedStore = "store-hand-edited";

    private readonly MigratedCatalogFixture _fixture;

    public ReclassifyPaymentMethodKindsMigrationTests(MigratedCatalogFixture fixture) =>
        _fixture = fixture;

    private PaymentMethodKind KindOf(string storeId, string code) =>
        (PaymentMethodKind)_fixture.Rows[(storeId, code)].Kind;

    [Theory]
    [InlineData("PayLater")]
    [InlineData("WelfareCard")]
    public void Migrate_OnCatalogPredatingReclassification_ShouldHaveStartedFromTheLegacyKind(string code)
    {
        // Guards the test itself: if the planted rows did not carry the pre-change kind, the
        // assertions below would agree with the end state without the migration doing anything.
        _fixture.RowsBefore[(LegacyStore, code)].Kind.Should().Be(1);
    }

    [Fact]
    public void Migrate_OnCatalogPredatingReclassification_ShouldRepointPayLaterToSpecial()
    {
        KindOf(LegacyStore, "PayLater").Should().Be(PaymentMethodKind.Special);
    }

    [Fact]
    public void Migrate_OnCatalogPredatingReclassification_ShouldRepointWelfareCardToGovernmentCampaign()
    {
        KindOf(LegacyStore, "WelfareCard").Should().Be(PaymentMethodKind.GovernmentCampaign);
    }

    [Theory]
    [InlineData("Cash")]
    [InlineData("MoneyTransfer")]
    public void Migrate_OnEverydayTender_ShouldLeaveKindAsStandard(string code)
    {
        // Standard reuses Permanent's backing value, so these rows must not be touched at all.
        KindOf(LegacyStore, code).Should().Be(PaymentMethodKind.Standard);
    }

    [Theory]
    [InlineData("M33WeLove")]
    [InlineData("FiftyFifty")]
    [InlineData("WeWin")]
    public void Migrate_OnExistingCampaigns_ShouldLeaveKindUnchanged(string code)
    {
        KindOf(LegacyStore, code).Should().Be(PaymentMethodKind.GovernmentCampaign);
    }

    [Fact]
    public void Migrate_OnReclassifiedWelfareCard_ShouldLeaveItEnabled()
    {
        // Kind is independent of enablement: the welfare card is a live payment method that
        // merely happens to be a government scheme. Reclassifying must not disable it.
        _fixture.Rows[(LegacyStore, "WelfareCard")].IsEnabled.Should().BeTrue();
    }

    [Theory]
    [InlineData("PayLater", PaymentMethodKind.Special)]
    [InlineData("WelfareCard", PaymentMethodKind.GovernmentCampaign)]
    public void Migrate_OnRowsAlreadyReclassified_ShouldLeaveThemUnchanged(
        string code, PaymentMethodKind expectedKind)
    {
        // The kind guard in the WHERE clause makes a re-run a no-op and spares any row an
        // admin has since re-classified by hand.
        KindOf(HandEditedStore, code).Should().Be(expectedKind);
    }

    /// <summary>
    /// Migrates a real PostgreSQL database to the revision *before* the reclassification,
    /// plants catalog rows carrying the pre-change kinds, then migrates to latest.
    /// Arrange + Act happen once here; each test asserts one aspect of the result.
    /// </summary>
    public sealed class MigratedCatalogFixture : IAsyncLifetime
    {
        /// <summary>The revision that introduced the payment_method table's last data fix-up.</summary>
        private const string MigrationBeforeReclassify = "20260719051546_MapLegacyPaymentValuesToCodes";

        private const int LegacyPermanent = 1;
        private const int LegacyGovernmentCampaign = 2;
        private const int AlreadySpecial = 3;

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("storehub_migration_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        public Dictionary<(string StoreId, string Code), (int Kind, bool IsEnabled)> Rows { get; } = new();

        /// <summary>State captured after planting rows but before migrating, so a test can prove
        /// the migration performed the transition rather than merely agreeing with the end state.</summary>
        public Dictionary<(string StoreId, string Code), (int Kind, bool IsEnabled)> RowsBefore { get; } = new();

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();

            await using var context = NewContext();
            var migrator = context.GetService<IMigrator>();

            await migrator.MigrateAsync(MigrationBeforeReclassify);
            await PlantPreChangeCatalogAsync(context);
            await LoadRowsAsync(RowsBefore);

            await migrator.MigrateAsync();

            await LoadRowsAsync(Rows);
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        private StoreHubDbContext NewContext() =>
            new(new DbContextOptionsBuilder<StoreHubDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options);

        /// <summary>
        /// Inserts via raw SQL on purpose: the entity's enum no longer has a Permanent member,
        /// so the pre-change state is only expressible as the persisted integer.
        /// </summary>
        private static Task PlantPreChangeCatalogAsync(StoreHubDbContext context) =>
            context.Database.ExecuteSqlRawAsync(
                $"""
                INSERT INTO payment_method
                    (store_id, code, display_name, kind, is_enabled, display_order, created_utc, last_modified_utc)
                VALUES
                    ('{LegacyStore}', 'Cash',          'เงินสด',              {LegacyPermanent},          true,  1, now(), now()),
                    ('{LegacyStore}', 'MoneyTransfer', 'เงินโอน',              {LegacyPermanent},          true,  2, now(), now()),
                    ('{LegacyStore}', 'WelfareCard',   'บัตรสวัสดิการแห่งรัฐ', {LegacyPermanent},          true,  3, now(), now()),
                    ('{LegacyStore}', 'PayLater',      'ลงบัญชี',              {LegacyPermanent},          true,  4, now(), now()),
                    ('{LegacyStore}', 'M33WeLove',     'ม33เรารักกัน',         {LegacyGovernmentCampaign}, false, 5, now(), now()),
                    ('{LegacyStore}', 'FiftyFifty',    'คนละครึ่ง',            {LegacyGovernmentCampaign}, false, 6, now(), now()),
                    ('{LegacyStore}', 'WeWin',         'เราชนะ',               {LegacyGovernmentCampaign}, false, 7, now(), now()),
                    ('{HandEditedStore}', 'PayLater',    'ลงบัญชี',              {AlreadySpecial},           true,  4, now(), now()),
                    ('{HandEditedStore}', 'WelfareCard', 'บัตรสวัสดิการแห่งรัฐ', {LegacyGovernmentCampaign}, true,  3, now(), now());
                """);

        private async Task LoadRowsAsync(
            Dictionary<(string StoreId, string Code), (int Kind, bool IsEnabled)> into)
        {
            await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
            await connection.OpenAsync();

            await using var command = new NpgsqlCommand(
                "SELECT store_id, code, kind, is_enabled FROM payment_method", connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                into[(reader.GetString(0), reader.GetString(1))] =
                    (reader.GetInt32(2), reader.GetBoolean(3));
            }
        }
    }
}
