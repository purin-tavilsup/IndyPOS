namespace IndyPOS.Application.UseCases.Cloud.Sync.Heartbeat;

// The heartbeat is a contract between StoreHub and CloudApi releases that are not upgraded together:
// both sides ignore fields they do not know, and no field may become required.

/// <summary>One outbox event the cloud refused, as the store reports it.</summary>
public sealed record FailedEventSummary(Guid EventId, string EventType, DateTime? FailedAtUtc, string? Reason);

/// <summary>A resend request the store carried out: the events it moved back to Pending, and those it could not.</summary>
public sealed record CompletedResend(Guid RequestId, IReadOnlyList<Guid> MovedEventIds, IReadOnlyList<Guid> IgnoredEventIds);

/// <summary>
/// What a StoreHub sends every two minutes. Kept light: the two lists are null when empty, so a store with
/// nothing wrong sends only its counts. The oldest pending event goes as an age, not a time, so a till PC with a
/// wrong clock cannot make the store look behind or on time.
/// </summary>
public sealed record HeartbeatRequest(
    string? StoreHubVersion,
    int PendingCount,
    int FailedCount,
    int? OldestPendingAgeSeconds,
    IReadOnlyList<FailedEventSummary>? FailedEvents,
    IReadOnlyList<CompletedResend>? CompletedResends);

/// <summary>Resend the store's Failed events: one, or every one when <paramref name="EventId"/> is null.</summary>
public sealed record ResendInstruction(Guid RequestId, Guid? EventId);

public sealed record HeartbeatResponse(IReadOnlyList<ResendInstruction> Resends);

public static class HeartbeatLimits
{
    /// <summary>The oldest Failed events a heartbeat lists; the count still covers them all.</summary>
    public const int MaxFailedEventsPerHeartbeat = 50;

    /// <summary>A failed event's reason is cut to this in a heartbeat; the full reason stays in StoreHub.</summary>
    public const int MaxReasonLength = 200;
}
