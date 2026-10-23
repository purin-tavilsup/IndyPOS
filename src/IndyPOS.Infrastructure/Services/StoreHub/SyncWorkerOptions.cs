namespace IndyPOS.Infrastructure.Services.StoreHub;

public class SyncWorkerOptions
{
    public const string SectionName = "SyncWorker";

    public int BatchSize { get; set; } = 10;
    public int PollingIntervalSeconds { get; set; } = 5;
    public int BaseRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// The longest wait between two sends of an event the cloud did not store. There is no attempt
    /// limit: only the cloud's own refusal makes an event Failed, so an outage of any length is waited
    /// out, at least four sends an hour. Default: 900 seconds (15 minutes).
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 900;
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval (in seconds) between user sync attempts.
    /// Default: 300 seconds (5 minutes).
    /// </summary>
    public int UserSyncIntervalSeconds { get; set; } = 300;
}
