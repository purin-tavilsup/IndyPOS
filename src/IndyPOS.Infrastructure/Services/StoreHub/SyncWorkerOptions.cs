namespace IndyPOS.Infrastructure.Services.StoreHub;

public class SyncWorkerOptions
{
    public const string SectionName = "SyncWorker";

    public int BatchSize { get; set; } = 10;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int MaxRetries { get; set; } = 5;
    public int BaseRetryDelaySeconds { get; set; } = 30;
    public bool Enabled { get; set; } = true;
}
