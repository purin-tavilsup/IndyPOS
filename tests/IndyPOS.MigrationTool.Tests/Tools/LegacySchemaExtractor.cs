using System.Data.SQLite;
using System.Text;
using Dapper;

namespace IndyPOS.MigrationTool.Tests.Tools;

/// <summary>
/// Which real store a legacy schema artefact was dumped from. The two shapes differ only by the
/// PayLater feature: GeneralHardware adds PayLater, Customers and Installments.
/// </summary>
public enum LegacyStoreShape
{
    /// <summary>13 tables. Has PayLater.</summary>
    GeneralHardware,

    /// <summary>10 tables. No PayLater. MimyMart shares this shape.</summary>
    MimyShop
}

/// <summary>
/// Manual developer tool. Dumps the legacy SQLite DDL from a real Store.db into a committed
/// .sql artefact, so tests never hand-write the legacy schema.
///
/// Hand-writing it is exactly what produced a fixture schema no store has -- see
/// docs/superpowers/specs/2026-08-03-migration-test-coverage-design.md.
///
/// SCHEMA ONLY. This must never copy rows: IndyPOS is a public repository.
///
/// Run with:
///   dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"
/// </summary>
public class LegacySchemaExtractor
{
    private static string SourceDb(LegacyStoreShape shape) => RealStoreDatabases.PathFor(shape);

    private static string ArtefactPath(LegacyStoreShape shape) => Path.Combine(
        RealStoreDatabases.RepoRoot, "tests", "IndyPOS.MigrationTool.Tests",
        "LegacySchema", $"{shape}.sql");

    [Fact(Skip = "Manual tool. Run explicitly to regenerate LegacySchema/*.sql from a real Store.db.")]
    public async Task ExtractLegacySchema()
    {
        foreach (var shape in Enum.GetValues<LegacyStoreShape>())
        {
            var source = SourceDb(shape);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(
                    $"Real store database not found for {shape}: {source}. " +
                    "These files are gitignored; obtain them before regenerating the artefacts.",
                    source);
            }

            await File.WriteAllTextAsync(ArtefactPath(shape), await BuildArtefactAsync(shape, source));
        }
    }

    private static async Task<string> BuildArtefactAsync(LegacyStoreShape shape, string sourceDbPath)
    {
        await using var source = new SQLiteConnection($"Data Source={sourceDbPath};Version=3;");
        await source.OpenAsync();

        var tables = (await source.QueryAsync<(string Name, string? Sql)>("""
            SELECT name AS Name, sql AS Sql
            FROM sqlite_master
            WHERE type = 'table' AND name NOT LIKE 'sqlite_%'
            ORDER BY name
            """)).ToList();

        var artefact = new StringBuilder();
        artefact.AppendLine($"-- GENERATED FILE -- DO NOT HAND-EDIT.");
        artefact.AppendLine($"-- Legacy SQLite schema dumped from a real {shape} Store.db.");
        artefact.AppendLine($"-- Regenerate with:");
        artefact.AppendLine($"--   dotnet test tests/IndyPOS.MigrationTool.Tests --filter \"ExtractLegacySchema\"");
        artefact.AppendLine($"-- Tables: {tables.Count}");
        artefact.AppendLine($"-- Schema only. Never add rows: this repository is public.");
        artefact.AppendLine();

        foreach (var (name, sql) in tables)
        {
            if (string.IsNullOrWhiteSpace(sql)) continue;
            artefact.AppendLine($"-- {name}");
            artefact.AppendLine($"{sql.TrimEnd()};");
            artefact.AppendLine();
        }

        return artefact.ToString();
    }
}
