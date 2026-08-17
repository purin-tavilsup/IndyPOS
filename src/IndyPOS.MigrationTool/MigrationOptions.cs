namespace IndyPOS.MigrationTool;

public class MigrationOptions
{
    public required string SqlitePath { get; init; }
    public required string PostgresConnectionString { get; init; }
    public required string StoreId { get; init; }
    public string? CloudApiUrl { get; init; }
    public string? CloudClientId { get; init; }
    public string? CloudClientSecret { get; init; }
    public bool DryRun { get; init; }

    /// <summary>
    /// How many invoices to accumulate before flushing them to PostgreSQL inside the migration's
    /// transaction. Lower uses less memory and more round trips.
    /// </summary>
    /// <remarks>
    /// The whole migration used to be held in memory until a single <c>SaveChangesAsync</c>, which
    /// peaked at **2.8 GB** on GeneralHardware — measured — against a documented minimum till spec of
    /// **4 GB** that also has to run Windows, PostgreSQL, StoreHub and the till app. Flushing in
    /// batches bounds that.
    /// <para>
    /// All-or-nothing is unaffected, because the flushes happen inside ONE transaction that is only
    /// committed if every phase succeeded — see <c>MigrateAllAsync</c>. Defect 12's guarantee is now
    /// explicit rather than a side effect of saving exactly once.
    /// </para>
    /// <para>Exists as an option so a test can set it to 2 and prove a flushed batch still rolls back.</para>
    /// </remarks>
    public int InvoiceFlushBatchSize { get; init; } = 5_000;
}
