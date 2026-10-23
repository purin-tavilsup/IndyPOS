namespace IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;

public enum StoreSyncState
{
    Ok,
    Behind,
    FailedEvents,
    Offline
}

public sealed record StoreSyncHealthOptions
{
    public const string SectionName = "SyncHealth";

    /// <summary>A store seen within this many minutes is online.</summary>
    public int OnlineWindowMinutes { get; init; } = 10;

    /// <summary>An online store whose oldest pending event is at least this old is behind.</summary>
    public int BehindAfterMinutes { get; init; } = 30;
}

/// <summary>A store's sync state for the admin page, from its latest heartbeat and the cloud's rejection log.</summary>
public static class StoreSyncHealth
{
    /// <remarks>
    /// Failed events win over everything: a store can be offline and still have events waiting for a resend.
    /// A rejection counts only when it is newer than the latest heartbeat, because the store is the source of
    /// truth for its own Failed events and a newer heartbeat already includes what happened to it.
    /// Offline is not an alarm: store PCs are switched off at night.
    /// </remarks>
    public static StoreSyncState Evaluate(
        DateTime? lastSeenUtc,
        int failedCount,
        DateTime? oldestPendingCreatedAtUtc,
        DateTime? newestUnresolvedRejectionUtc,
        DateTime nowUtc,
        StoreSyncHealthOptions options)
    {
        var rejectedSinceLastSeen = newestUnresolvedRejectionUtc is { } rejected && (lastSeenUtc is null || rejected > lastSeenUtc);

        if (failedCount > 0 || rejectedSinceLastSeen)
            return StoreSyncState.FailedEvents;

        if (lastSeenUtc is null || nowUtc - lastSeenUtc >= TimeSpan.FromMinutes(options.OnlineWindowMinutes))
            return StoreSyncState.Offline;

        return oldestPendingCreatedAtUtc is { } oldest && nowUtc - oldest >= TimeSpan.FromMinutes(options.BehindAfterMinutes)
            ? StoreSyncState.Behind
            : StoreSyncState.Ok;
    }
}
