namespace IndyPOS.Application.Abstractions.StoreHub.Services;

public enum SyncSendOutcome
{
    /// <summary>The cloud holds the event (stored now, or already had it).</summary>
    Sent,

    /// <summary>Not stored this time, for a reason that may pass: send it again later.</summary>
    RetryLater,

    /// <summary>The cloud refused the event on purpose. Sending it again unchanged cannot succeed.</summary>
    Refused
}

/// <summary>What happened to one outbox event sent to the cloud, and why when it was not stored.</summary>
public sealed record SyncSendResult(SyncSendOutcome Outcome, string? Reason = null)
{
    public static readonly SyncSendResult Sent = new(SyncSendOutcome.Sent);

    public static SyncSendResult RetryLater(string reason) => new(SyncSendOutcome.RetryLater, reason);

    public static SyncSendResult Refused(string reason) => new(SyncSendOutcome.Refused, reason);
}
