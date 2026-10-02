using FluentAssertions;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Migrations;

/// <summary>
/// Exercises AddInvoiceNumber's backfill against a real database that already holds both kinds of
/// invoice. IntegrationTestBase uses EnsureCreated, which never runs a migration, so this is the only
/// coverage of the backfill order: legacy ids, then setval, then native rows by created_utc.
/// </summary>
public class AddInvoiceNumberMigrationTests
    : IClassFixture<AddInvoiceNumberMigrationTests.BackfilledInvoicesFixture>
{
    private const long HighestLegacyNumber = 7005;

    private readonly BackfilledInvoicesFixture _fixture;

    public AddInvoiceNumberMigrationTests(BackfilledInvoicesFixture fixture) => _fixture = fixture;

    private long? NumberOf(Guid id) => _fixture.Rows[id].InvoiceNumber;

    [Fact]
    public void Migrate_WithBothKinds_LeavesNoInvoiceWithoutANumber()
    {
        _fixture.Rows.Values.Should()
                            .OnlyContain(row => row.InvoiceNumber != null);
    }

    [Fact]
    public void Migrate_WithBothKinds_LeavesNoDuplicateNumberInAStore()
    {
        _fixture.Rows.Values.GroupBy(row => (row.StoreId, row.InvoiceNumber))
                            .Should()
                            .OnlyContain(group => group.Count() == 1);
    }

    [Fact]
    public void Migrate_WithNativeInvoicesInsertedNewestFirst_NumbersTheOlderOneFirst()
    {
        // The newer row was INSERTed first, so physical order alone would number it first.
        NumberOf(BackfilledInvoicesFixture.OlderNative).Should()
                                                       .Be(HighestLegacyNumber + 1);
    }

    [Fact]
    public void Migrate_WithNativeInvoices_NumbersTheNewerOneAfterTheOlder()
    {
        NumberOf(BackfilledInvoicesFixture.NewerNative).Should()
                                                       .Be(HighestLegacyNumber + 2);
    }

    [Fact]
    public void Migrate_WithBothKinds_MovesTheSequencePastTheMaximum()
    {
        _fixture.NextValue.Should()
                          .Be(HighestLegacyNumber + 3);
    }

    [Fact]
    public void Migrate_WithALegacyInvoice_KeepsItsV3Number()
    {
        NumberOf(BackfilledInvoicesFixture.Legacy7001StoreA).Should()
                                                            .Be(7001);
    }

    [Fact]
    public void Migrate_WithTheSameLegacyIdInAnotherStore_KeepsItsV3NumberThereToo()
    {
        NumberOf(BackfilledInvoicesFixture.Legacy7001StoreB).Should()
                                                            .Be(7001);
    }

    [Fact]
    public void Migrate_OnAnEmptyInvoiceTable_StartsNumberingAtOne()
    {
        // setval must be skipped when there is no maximum: setval(seq, NULL) would fail the migration.
        _fixture.FirstNumberOnEmptyTable.Should()
                                        .Be(1);
    }

    /// <summary>
    /// Migrates one database to the revision before AddInvoiceNumber, plants legacy and native
    /// invoices, then migrates to latest. A second, empty database proves the empty-table path.
    /// </summary>
    public sealed class BackfilledInvoicesFixture : IAsyncLifetime
    {
        /// <summary>The last migration of the previous (cash-drawer) release.</summary>
        private const string MigrationBeforeInvoiceNumber = "20260926152602_AddCashDrawerTables";

        private const string EmptyDatabase = "storehub_empty";

        private const string NextValueSql =
            $"SELECT CASE WHEN is_called THEN last_value + 1 ELSE last_value END FROM {InvoiceNumberSequence.Name}";

        public static readonly Guid Legacy7001StoreA = Guid.Parse("10000000-0000-0000-0000-000000007001");
        public static readonly Guid Legacy7005StoreA = Guid.Parse("10000000-0000-0000-0000-000000007005");
        public static readonly Guid Legacy7001StoreB = Guid.Parse("20000000-0000-0000-0000-000000007001");
        public static readonly Guid OlderNative = Guid.Parse("30000000-0000-0000-0000-000000000001");
        public static readonly Guid NewerNative = Guid.Parse("30000000-0000-0000-0000-000000000002");

        private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("storehub_backfill_test")
            .WithUsername("test_user")
            .WithPassword("test_password")
            .Build();

        public Dictionary<Guid, (string StoreId, long? InvoiceNumber)> Rows { get; } = new();

        public long NextValue { get; private set; }

        public long FirstNumberOnEmptyTable { get; private set; }

        public async Task InitializeAsync()
        {
            await _postgres.StartAsync();
            var connectionString = _postgres.GetConnectionString();

            await using (var context = NewContext(connectionString))
            {
                var migrator = context.GetService<IMigrator>();
                await migrator.MigrateAsync(MigrationBeforeInvoiceNumber);
                await context.Database.ExecuteSqlRawAsync(PlantedInvoicesSql);
                await migrator.MigrateAsync();
            }

            await LoadRowsAsync(connectionString);
            NextValue = await ScalarAsync<long>(connectionString, NextValueSql);
            FirstNumberOnEmptyTable = await MigrateEmptyDatabaseAndInsertAsync(connectionString);
        }

        public async Task DisposeAsync() => await _postgres.DisposeAsync();

        /// <summary>
        /// Pre-release columns only (no invoice_number yet). The newer native row goes in FIRST, so
        /// a backfill that numbered by physical order instead of created_utc would fail the tests.
        /// </summary>
        private static string PlantedInvoicesSql => $"""
            INSERT INTO invoice (id, store_id, user_id, total_amount, created_utc, last_modified_utc, legacy_invoice_id)
            VALUES
              ('{Legacy7001StoreA}', 'store-a', gen_random_uuid(), 120, '2024-03-15 07:30:00+00', '2024-03-15 07:30:00+00', 7001),
              ('{Legacy7005StoreA}', 'store-a', gen_random_uuid(),  80, '2024-03-16 07:30:00+00', '2024-03-16 07:30:00+00', 7005),
              ('{Legacy7001StoreB}', 'store-b', gen_random_uuid(),  60, '2024-03-15 08:00:00+00', '2024-03-15 08:00:00+00', 7001),
              ('{NewerNative}',      'store-a', gen_random_uuid(),  50, '2026-09-25 03:00:00+00', '2026-09-25 03:00:00+00', NULL),
              ('{OlderNative}',      'store-a', gen_random_uuid(),  40, '2026-09-20 03:00:00+00', '2026-09-20 03:00:00+00', NULL);
            """;

        private static StoreHubDbContext NewContext(string connectionString) =>
            new(new DbContextOptionsBuilder<StoreHubDbContext>()
                .UseNpgsql(connectionString)
                .Options);

        private async Task LoadRowsAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand("SELECT id, store_id, invoice_number FROM invoice", connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                Rows[reader.GetGuid(0)] = (reader.GetString(1), reader.IsDBNull(2) ? null : reader.GetInt64(2));
            }
        }

        private static async Task<long> MigrateEmptyDatabaseAndInsertAsync(string connectionString)
        {
            await ScalarAsync<object>(connectionString, $"CREATE DATABASE {EmptyDatabase}");
            var emptyConnectionString = new NpgsqlConnectionStringBuilder(connectionString) { Database = EmptyDatabase }.ConnectionString;

            await using (var context = NewContext(emptyConnectionString))
            {
                await context.Database.MigrateAsync();
            }

            return await ScalarAsync<long>(emptyConnectionString, """
                INSERT INTO invoice (id, store_id, user_id, total_amount, created_utc, last_modified_utc)
                VALUES (gen_random_uuid(), 'store-a', gen_random_uuid(), 10, now(), now())
                RETURNING invoice_number
                """);
        }

        private static async Task<T> ScalarAsync<T>(string connectionString, string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            return (T)(await command.ExecuteScalarAsync())!;
        }
    }
}
