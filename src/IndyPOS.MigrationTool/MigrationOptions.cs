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
}
