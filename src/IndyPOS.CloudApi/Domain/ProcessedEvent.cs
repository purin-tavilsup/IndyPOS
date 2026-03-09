namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// Tracks processed events for idempotency.
/// Ensures events are processed exactly once even if delivered multiple times.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
