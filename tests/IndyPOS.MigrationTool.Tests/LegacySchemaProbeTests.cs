using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Services;

namespace IndyPOS.MigrationTool.Tests;

/// <summary>
/// Pure unit tests - no Docker, no PostgreSQL. The probe only ever talks to SQLite.
/// </summary>
public class LegacySchemaProbeTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"probe-{Guid.NewGuid():N}.db");
    private SQLiteConnection _sqlite = null!;

    public async Task InitializeAsync()
    {
        _sqlite = new SQLiteConnection($"Data Source={_path};Version=3;");
        await _sqlite.OpenAsync();
    }

    public async Task DisposeAsync()
    {
        await _sqlite.DisposeAsync();
        SQLiteConnection.ClearAllPools();
        File.Delete(_path);
    }

    [Fact]
    public async Task HasTableAsync_WhenTheTableExists_ShouldReturnTrue()
    {
        await _sqlite.ExecuteAsync("CREATE TABLE PayLater (PaymentId INTEGER PRIMARY KEY)");

        (await LegacySchemaProbe.HasTableAsync(_sqlite, "PayLater")).Should().BeTrue();
    }

    [Fact]
    public async Task HasTableAsync_WhenTheTableIsAbsent_ShouldReturnFalse()
    {
        await _sqlite.ExecuteAsync("CREATE TABLE Invoice (InvoiceId INTEGER PRIMARY KEY)");

        (await LegacySchemaProbe.HasTableAsync(_sqlite, "PayLater")).Should().BeFalse();
    }

    [Theory]
    [InlineData("paylater")]
    [InlineData("PAYLATER")]
    [InlineData("PayLater")]
    public async Task HasTableAsync_WithAnyCasingOfAnExistingTable_ShouldReturnTrue(string declaredName)
    {
        // The probe must be exactly as permissive as the SELECT it guards. SQLite resolves table
        // names in a query case-insensitively, but `sqlite_master.name` collates BINARY - so a
        // plain `=` would report a differently-cased table as ABSENT while `SELECT ... FROM
        // PayLater` against it would have succeeded. The check would then be skipped rather than
        // run, which is the silent-disappearance failure the probe exists to prevent.
        await _sqlite.ExecuteAsync($"CREATE TABLE {declaredName} (PaymentId INTEGER PRIMARY KEY)");

        (await LegacySchemaProbe.HasTableAsync(_sqlite, "PayLater")).Should().BeTrue(
            $"a table declared as '{declaredName}' is readable as 'PayLater'");
    }

    [Fact]
    public async Task HasTableAsync_ForAView_ShouldReturnFalse()
    {
        // Deliberate: the callers go on to run a plain COUNT(*), which works against a view too,
        // so this is arguably strict. It stays strict because every legacy artefact dumped from a
        // real store declares these as tables - treating a view as present would be inventing a
        // shape no store has, the mistake that produced defects 2 and 3.
        await _sqlite.ExecuteAsync("CREATE TABLE Payment (PaymentId INTEGER PRIMARY KEY)");
        await _sqlite.ExecuteAsync("CREATE VIEW PayLater AS SELECT * FROM Payment");

        (await LegacySchemaProbe.HasTableAsync(_sqlite, "PayLater")).Should().BeFalse();
    }

    [Fact]
    public async Task HasTableAsync_WithAQuoteInTheName_ShouldNotThrow()
    {
        // Parameterised, not interpolated. A table name is developer-supplied today, but the whole
        // point of a shared probe is that the next caller may not be.
        (await LegacySchemaProbe.HasTableAsync(_sqlite, "Pay'Later")).Should().BeFalse();
    }
}
