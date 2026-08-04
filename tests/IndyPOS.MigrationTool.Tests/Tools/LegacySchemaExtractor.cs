using System.Data.SQLite;
using System.Text;
using Dapper;

namespace IndyPOS.MigrationTool.Tests.Tools;

/// <summary>
/// Marks a manual developer tool that ships as a test. It is skipped unless the named environment
/// variable is set, so a normal <c>dotnet test</c> never runs it.
///
/// Not <c>[Fact(Skip = "...")]</c>: a static skip cannot be lifted by <c>--filter</c>, so the
/// documented way to run the tool would silently do nothing. xUnit 2.9.3 has no runtime skip and no
/// "explicit test" support, so the decision is made here, at discovery.
/// </summary>
public sealed class ManualToolFactAttribute : FactAttribute
{
    public ManualToolFactAttribute(string environmentVariable, string reason)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(environmentVariable)))
        {
            Skip = $"{reason} Set {environmentVariable}=1 to run it.";
        }
    }
}

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
/// Run with (PowerShell):
///   $env:INDYPOS_REGENERATE_LEGACY_SCHEMA = "1"
///   dotnet test tests/IndyPOS.MigrationTool.Tests --filter "ExtractLegacySchema"
///
/// The environment variable is REQUIRED. `--filter` selects a test; it does not un-skip one, so a
/// statically skipped <c>[Fact(Skip = "...")]</c> would report "Skipped: 1" and write nothing --
/// which reads as success while leaving the artefacts untouched.
/// </summary>
public class LegacySchemaExtractor
{
    internal const string EnvironmentVariable = "INDYPOS_REGENERATE_LEGACY_SCHEMA";

    private static string SourceDb(LegacyStoreShape shape) => RealStoreDatabases.PathFor(shape);

    private static string ArtefactPath(LegacyStoreShape shape) => Path.Combine(
        RealStoreDatabases.RepoRoot, "tests", "IndyPOS.MigrationTool.Tests",
        "LegacySchema", $"{shape}.sql");

    [ManualToolFact(EnvironmentVariable,
        "Manual tool. Regenerates LegacySchema/*.sql from a real Store.db.")]
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
        artefact.AppendLine($"-- Regenerate with (PowerShell):");
        artefact.AppendLine($"--   $env:{EnvironmentVariable} = \"1\"");
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
