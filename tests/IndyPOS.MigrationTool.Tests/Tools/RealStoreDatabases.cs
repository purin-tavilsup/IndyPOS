namespace IndyPOS.MigrationTool.Tests.Tools;

/// <summary>
/// Locates the real store databases. They are gitignored and ~64 MB, so they exist only on a
/// machine holding real store data -- never in CI.
/// </summary>
public static class RealStoreDatabases
{
    /// <remarks>
    /// A hardcoded 5-level walk from the build output. It fails LOUDLY (a missing file naming the
    /// path) rather than silently, and every caller is gated on a real database being present.
    /// </remarks>
    public static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public static string PathFor(LegacyStoreShape shape) => Path.Combine(
        RepoRoot, ".planning", "indypos-overhaul", "sqlite_database", shape.ToString(), "Store.db");

    public static bool IsMissing(LegacyStoreShape shape) => !File.Exists(PathFor(shape));

    /// <returns>A skip reason, or <c>null</c> when every requested shape is present.</returns>
    public static string? SkipReasonFor(params LegacyStoreShape[] shapes)
    {
        var missing = shapes.Where(IsMissing).ToList();
        if (missing.Count == 0) return null;

        return $"No real Store.db for {string.Join(", ", missing)} of the required " +
               $"{string.Join(", ", shapes)} (gitignored, ~64 MB each). " +
               "SKIPPED, not passed: this check can only run on a machine holding real store data. " +
               $"Expected at {PathFor(missing[0])}";
    }
}

/// <summary>
/// A <see cref="FactAttribute"/> that reports SKIPPED -- never Passed -- when the real store
/// databases are absent.
///
/// xUnit 2.9.3 has no runtime skip (<c>Assert.Skip</c> arrived in v3), so the decision is made at
/// discovery time, where <see cref="FactAttribute.Skip"/> is still settable. An early
/// <c>return;</c> is the vacuous-pass bug this whole file exists to remove.
/// </summary>
public sealed class RealStoreFactAttribute : FactAttribute
{
    public RealStoreFactAttribute(params LegacyStoreShape[] shapes)
    {
        var required = shapes.Length > 0 ? shapes : Enum.GetValues<LegacyStoreShape>();
        if (RealStoreDatabases.SkipReasonFor(required) is { } reason) Skip = reason;
    }
}

/// <summary>
/// <see cref="RealStoreFactAttribute"/> for theories. Requires EVERY shape, and the skip covers the
/// whole theory: a discovery-time decision cannot discriminate between individual data rows, so a
/// machine holding only some shapes skips the cases it could have run.
/// </summary>
public sealed class RealStoreTheoryAttribute : TheoryAttribute
{
    public RealStoreTheoryAttribute()
    {
        if (RealStoreDatabases.SkipReasonFor(Enum.GetValues<LegacyStoreShape>()) is { } reason)
            Skip = reason;
    }
}
