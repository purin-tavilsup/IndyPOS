using System.Data.SQLite;
using Dapper;

namespace IndyPOS.MigrationTool.Services;

/// <summary>
/// The outcome of resolving one legacy category id: at most one of the two is non-null.
/// </summary>
/// <param name="Code">The v4 catalogue code, or <c>null</c> when there is none to write.</param>
/// <param name="Problem">
/// Why no code was produced, phrased as a cause the caller can prefix. <c>null</c> both when a code
/// WAS produced and when the legacy row is genuinely uncategorised -- an absent category is not a
/// data problem, and reporting it would bury the ones that are under the error cap.
/// </param>
public sealed record CategoryResolution(string? Code, string? Problem);

/// <summary>
/// Resolves one store's legacy category ids to v4 catalogue codes, via that store's OWN
/// <c>ProductCategory</c> lookup and then <see cref="LegacyCategoryMap"/>.
/// </summary>
/// <remarks>
/// <para>
/// Two hops rather than one, because the legacy id is not a stable key: id 10 is เบ็ดเตล็ด in
/// GeneralHardware and ของขวัญ in MimyShop. Only the Thai name means the same thing everywhere, so
/// the store resolves its own ids locally and the shared map is keyed on the name.
/// </para>
/// <para>
/// Shared by the migrator and the verifier deliberately, the same way
/// <see cref="LegacyPaymentTypeMap"/> is: the verifier compares the code written against the code
/// expected, and it can only do that honestly if it derives the expectation from the same lookup
/// the migration read.
/// </para>
/// </remarks>
public sealed class LegacyCategoryResolver
{
    private readonly Dictionary<long, string> _nameById;

    private LegacyCategoryResolver(Dictionary<long, string> nameById) => _nameById = nameById;

    /// <remarks>
    /// Not guarded by <see cref="LegacySchemaProbe"/>. Unlike PayLater, <c>ProductCategory</c> is
    /// part of every real store's schema, so a store without it is unknown territory: letting the
    /// query throw turns into a recorded phase failure that aborts the run and says why, which
    /// beats migrating every product uncategorised and reporting success.
    /// </remarks>
    public static async Task<LegacyCategoryResolver> LoadAsync(SQLiteConnection sqlite)
    {
        var rows = await sqlite.QueryAsync<LegacyCategoryRow>(
            "SELECT Id, Category FROM ProductCategory");

        return new LegacyCategoryResolver(rows.ToDictionary(row => row.Id, row => row.Category));
    }

    /// <returns>
    /// A code, or <c>null</c> plus a problem. Never a guess and never the raw id: an id matches no
    /// catalogue code, so it leaves the product unreachable from the Hardware gate and every
    /// category picker while looking populated -- which is what defect 5 was.
    /// </returns>
    public CategoryResolution Resolve(long? legacyCategoryId)
    {
        if (legacyCategoryId is not { } id) return new CategoryResolution(null, null);

        if (!_nameById.TryGetValue(id, out var legacyName))
        {
            return new CategoryResolution(null,
                $"legacy category id {id} is not in this store's ProductCategory table.");
        }

        if (LegacyCategoryMap.ToCode(legacyName) is not { } code)
        {
            return new CategoryResolution(null,
                $"legacy category '{legacyName}' has no catalogue code -- add it to " +
                "LegacyCategoryMap and ProductCategoryCodes.");
        }

        return new CategoryResolution(code, null);
    }

    private sealed class LegacyCategoryRow
    {
        public long Id { get; set; }
        public string Category { get; set; } = "";
    }
}
