using System.Data.SQLite;
using Dapper;

namespace IndyPOS.MigrationTool.Services;

/// <summary>
/// Asks the legacy SQLite catalogue whether an optional table exists.
/// </summary>
/// <remarks>
/// <para>
/// Shared on purpose. The three real stores do not share one schema - PayLater is a
/// GeneralHardware-only feature - so both the migrator and the verifier have to ask before they
/// read. Defect 3 fixed that in the migrator alone, and defect 15 was the same bug surviving in
/// the verifier for months afterwards. One implementation means the next optional table, and any
/// correction to the probe itself, cannot land in one caller and miss the other.
/// </para>
/// </remarks>
public static class LegacySchemaProbe
{
    /// <param name="tableName">
    /// Matched case-INSENSITIVELY, because that is how the SELECT it guards resolves the name.
    /// <c>sqlite_master.name</c> collates BINARY, so a plain <c>=</c> is stricter than the query
    /// being guarded: a store whose table is cased differently would be reported as not having it
    /// at all, and the check would be skipped rather than run - the silent-disappearance failure
    /// this probe exists to prevent.
    /// </param>
    public static async Task<bool> HasTableAsync(SQLiteConnection sqlite, string tableName)
    {
        var count = await sqlite.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = @tableName COLLATE NOCASE",
            new { tableName });

        return count > 0;
    }
}
