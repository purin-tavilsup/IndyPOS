namespace IndyPOS.Infrastructure.Services.StoreHub;

public class HeartbeatOptions
{
    public const string SectionName = "Heartbeat";

    /// <summary>Seconds between two heartbeats. Default: 120 (2 minutes).</summary>
    public int IntervalSeconds { get; set; } = 120;

    public bool Enabled { get; set; } = true;
}
