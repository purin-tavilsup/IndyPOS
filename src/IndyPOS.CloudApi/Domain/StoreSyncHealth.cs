namespace IndyPOS.CloudApi.Domain;

/// <summary>A store's latest heartbeat: one row per store, overwritten each time.</summary>
public class CloudStoreHeartbeat
{
    public string StoreId { get; set; } = string.Empty;
    public DateTime ReceivedAtUtc { get; set; }
    public string? StoreHubVersion { get; set; }
    public int PendingCount { get; set; }
    public int FailedCount { get; set; }
    public DateTime? OldestPendingCreatedAtUtc { get; set; }

    /// <summary>The store's Failed events as JSON (FailedEventSummary list), or null when it has none.</summary>
    public string? FailedEventsJson { get; set; }
}

/// <summary>A refusal the cloud gave on /sync/events, per store and event; resolved when that event is accepted.</summary>
public class CloudSyncRejection
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;

    /// <summary>Null when the refused batch named no event that could be read.</summary>
    public Guid? EventId { get; set; }

    public string? EventType { get; set; }
    public int StatusCode { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime FirstRejectedAtUtc { get; set; }
    public DateTime LastRejectedAtUtc { get; set; }
    public int Count { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
}

/// <summary>An admin's request that a store resend its Failed events, carried to the store in a heartbeat reply.</summary>
public class CloudResendRequest
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;

    /// <summary>The one event to resend, or null for every Failed event the store has.</summary>
    public Guid? EventId { get; set; }

    public DateTime RequestedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? MovedEventIdsJson { get; set; }
    public string? IgnoredEventIdsJson { get; set; }
}
