using System.Data.SQLite;
using Dapper;
using IndyPOS.MigrationTool.Tests.Tools;

namespace IndyPOS.MigrationTool.Tests.Fixtures;

/// <summary>
/// A throwaway SQLite database carrying a real store's legacy schema.
///
/// The schema comes from a generated artefact, never from hand-written DDL: a hand-written
/// fixture schema is what let defects 2, 3 and 6 pass unnoticed.
/// </summary>
public sealed class LegacyStoreDatabase : IAsyncDisposable
{
    private LegacyStoreDatabase(string path, SQLiteConnection connection)
    {
        Path = path;
        Connection = connection;
    }

    /// <summary>Absolute path to the temp file, for <c>MigrationOptions.SqlitePath</c>.</summary>
    public string Path { get; }

    /// <summary>An open connection, for seeding and for reading legacy rows back.</summary>
    public SQLiteConnection Connection { get; }

    public static async Task<LegacyStoreDatabase> CreateAsync(LegacyStoreShape shape)
    {
        var path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"legacy_{shape}_{Guid.NewGuid():N}.db");

        var connection = new SQLiteConnection($"Data Source={path};Version=3;");
        await connection.OpenAsync();

        var ddlPath = System.IO.Path.Combine(AppContext.BaseDirectory, "LegacySchema", $"{shape}.sql");
        if (!File.Exists(ddlPath))
        {
            throw new FileNotFoundException(
                $"Legacy schema artefact missing: {ddlPath}. Is LegacySchema\\*.sql copied to output?",
                ddlPath);
        }

        await connection.ExecuteAsync(await File.ReadAllTextAsync(ddlPath));

        // Reference data, not per-test data: every real store ships its ProductCategory rows with
        // the schema. Seeding it here rather than per test stops a category-resolution bug passing
        // against an empty lookup while every real store fails.
        await LegacyCategoryLookup.SeedAsync(connection, shape);

        return new LegacyStoreDatabase(path, connection);
    }

    public async ValueTask DisposeAsync()
    {
        await Connection.CloseAsync();
        Connection.Dispose();

        // SQLite holds the file until pooled handles are released.
        // Scoped to THIS connection's pool. ClearAllPools() would reach every SQLite
        // connection in the process, and this fixture is used from several test classes
        // that xUnit runs in parallel collections.
        SQLiteConnection.ClearPool(Connection);

        if (File.Exists(Path))
        {
            try { File.Delete(Path); }
            catch (IOException) { /* a leaked handle must not fail an otherwise-passing test */ }
        }
    }
}
