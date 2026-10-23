namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Outbox event for reliable cloud sync.
/// Written in same transaction as business data, processed by SyncWorker.
/// </summary>
public class OutboxEvent
{
    /// <summary>The longest <see cref="LastError"/> stored; a longer reason is cut to fit the column.</summary>
    public const int LastErrorMaxLength = 1000;

    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public string Type { get; set; } = default!;  // InvoiceCompleted, InventoryMovementRecorded, etc.
    public string PayloadJson { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public DateTime? NextRetryUtc { get; set; }
    public string Status { get; set; } = "Pending";  // Pending, Sent, Failed

    /// <summary>
    /// Why the last attempt did not succeed: the cloud's refusal for a Failed event, or the transient
    /// error of the latest retry. Null until an attempt fails.
    /// </summary>
    public string? LastError { get; set; }
}
