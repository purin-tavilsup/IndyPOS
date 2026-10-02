namespace IndyPOS.Application.UseCases.Cloud.Sync.Events;

/// <summary>A reprint was requested. The cloud mirrors it insert-only (plan 2).</summary>
public record InvoiceReprintedEvent
{
    public int SchemaVersion { get; init; } = 1;
    public Guid EventId { get; init; }
    public Guid ReprintId { get; init; }
    public Guid InvoiceId { get; init; }
    public string StoreId { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public Guid CreatedByUserId { get; init; }
}
