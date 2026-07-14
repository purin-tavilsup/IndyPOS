namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Outbox event for reliable cloud sync.
/// Written in same transaction as business data, processed by SyncWorker.
/// </summary>
public class OutboxEvent
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public string Type { get; set; } = default!;  // InvoiceCompleted, InventoryMovementRecorded, etc.
    public string PayloadJson { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttemptUtc { get; set; }
    public DateTime? NextRetryUtc { get; set; }
    public string Status { get; set; } = "Pending";  // Pending, Sent, Failed
}
