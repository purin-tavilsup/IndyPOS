using System.Data.SQLite;
using IndyPOS.MigrationTool.Services;
using IndyPOS.MigrationTool.Tests.Fixtures;
using IndyPOS.MigrationTool.Tests.TestData;
using Microsoft.Extensions.Logging.Abstractions;

namespace IndyPOS.MigrationTool.Tests;

[Collection("Postgres")]
public class MigrationVerifierTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private SQLiteConnection _sqliteConnection = null!;
    private string _sqliteDbPath = null!;

    public MigrationVerifierTests(PostgresFixture postgres)
    {
        _postgres = postgres;
    }

    public async Task InitializeAsync()
    {
        _sqliteDbPath = Path.Combine(Path.GetTempPath(), $"verify_test_{Guid.NewGuid()}.db");
        _sqliteConnection = new SQLiteConnection($"Data Source={_sqliteDbPath};Version=3;");
        await _sqliteConnection.OpenAsync();
        await _postgres.ResetDatabaseAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqliteConnection.CloseAsync();
        _sqliteConnection.Dispose();
        if (File.Exists(_sqliteDbPath)) File.Delete(_sqliteDbPath);
    }

    [Fact]
    public async Task VerifyAsync_AfterSuccessfulMigration_ReturnsValid()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        var data = await seeder.SeedCompleteDataSetAsync(userCount: 3, productCount: 15, invoiceCount: 30);

        // Perform migration
        var migrationOptions = CreateOptions();
        var migrationService = new SqliteMigrationService(migrationOptions, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Act
        var verifier = new MigrationVerifier(migrationOptions, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();

        result.Checks.Should().Contain(c => c.EntityName == "Users" && c.IsValid);
        result.Checks.Should().Contain(c => c.EntityName == "Products" && c.IsValid);
        result.Checks.Should().Contain(c => c.EntityName == "Invoices" && c.IsValid);
        result.Checks.Should().Contain(c => c.EntityName == "Payments" && c.IsValid);
    }

    [Fact]
    public async Task VerifyAsync_BeforeMigration_ReturnsInvalid()
    {
        // Arrange - seed SQLite but don't migrate
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.SeedCompleteDataSetAsync(userCount: 3, productCount: 15, invoiceCount: 30);

        var options = CreateOptions();
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);

        // Act
        var result = await verifier.VerifyAsync();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();

        // All counts should show mismatch (SQLite > 0, PostgreSQL = 0)
        result.Checks.Where(c => c.EntityName == "Users").First().SqliteCount.Should().BeGreaterThan(0);
        result.Checks.Where(c => c.EntityName == "Users").First().PostgresCount.Should().Be(0);
    }

    [Fact]
    public async Task VerifyAsync_WithPartialMigration_ReportsDiscrepancies()
    {
        // Arrange - seed SQLite with initial data
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.SeedCompleteDataSetAsync(userCount: 5, productCount: 20, invoiceCount: 0);

        // Migrate
        var options = CreateOptions();
        var migrationService = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Now add more products to SQLite (simulating new data after migration)
        // Use products instead of users to avoid unique constraint collision on username
        await seeder.SeedProductsAsync(3);

        // Act
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert - Products should show mismatch (SQLite has more than PostgreSQL)
        var productCheck = result.Checks.First(c => c.EntityName == "Products");
        productCheck.SqliteCount.Should().Be(23); // 20 + 3
        productCheck.PostgresCount.Should().Be(20); // Only original 20
        productCheck.IsValid.Should().BeFalse(); // 23 <= 20 is false, so invalid
    }

    [Fact]
    public async Task VerifyAsync_TotalRevenue_MatchesWithinTolerance()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        await seeder.SeedCompleteDataSetAsync(userCount: 2, productCount: 10, invoiceCount: 50);

        var options = CreateOptions();
        var migrationService = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Act
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert
        var revenueCheck = result.Checks.First(c => c.EntityName == "Total Revenue");
        revenueCheck.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task VerifyAsync_ReturnsCorrectCounts()
    {
        // Arrange
        var seeder = new SqliteTestDataSeeder(_sqliteConnection);
        var data = await seeder.SeedCompleteDataSetAsync(userCount: 4, productCount: 12, invoiceCount: 25);

        var options = CreateOptions();
        var migrationService = new SqliteMigrationService(options, NullLogger<SqliteMigrationService>.Instance);
        await migrationService.MigrateAllAsync();

        // Act
        var verifier = new MigrationVerifier(options, NullLogger<MigrationVerifier>.Instance);
        var result = await verifier.VerifyAsync();

        // Assert
        result.Checks.First(c => c.EntityName == "Users").SqliteCount.Should().Be(4);
        result.Checks.First(c => c.EntityName == "Products").SqliteCount.Should().Be(12);
        result.Checks.First(c => c.EntityName == "Invoices").SqliteCount.Should().Be(25);

        result.Checks.First(c => c.EntityName == "Users").PostgresCount.Should().Be(4);
        result.Checks.First(c => c.EntityName == "Products").PostgresCount.Should().Be(12);
        result.Checks.First(c => c.EntityName == "Invoices").PostgresCount.Should().Be(25);
    }

    private MigrationOptions CreateOptions()
    {
        return new MigrationOptions
        {
            SqlitePath = _sqliteDbPath,
            PostgresConnectionString = _postgres.ConnectionString,
            StoreId = "TEST-STORE",
            DryRun = false
        };
    }
}
