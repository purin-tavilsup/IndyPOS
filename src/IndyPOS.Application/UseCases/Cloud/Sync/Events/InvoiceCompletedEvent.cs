namespace IndyPOS.Application.UseCases.Cloud.Sync.Events;

/// <summary>
/// Transaction snapshot event for a completed invoice.
/// Self-contained - includes all data needed to reconstruct the transaction in Cloud.
/// </summary>
public record InvoiceCompletedEvent
{
    /// <summary>
    /// Schema version for forward compatibility.
    /// Increment when making breaking changes to the event structure.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;

    // Correlation / Aggregate IDs
    public Guid EventId { get; init; }
    public Guid InvoiceId { get; init; }
    public string StoreId { get; init; } = string.Empty;
    public string? TerminalId { get; init; }

    // Invoice header
    public Guid UserId { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime CreatedAtUtc { get; init; }

    // Transaction details
    public IReadOnlyList<InvoiceLineSnapshot> Lines { get; init; } = [];
    public IReadOnlyList<PaymentSnapshot> Payments { get; init; } = [];
    public IReadOnlyList<InventoryMovementSnapshot> InventoryMovements { get; init; } = [];
}

/// <summary>
/// Snapshot of an invoice line at transaction time.
/// </summary>
public record InvoiceLineSnapshot
{
    public Guid LineId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

/// <summary>
/// Snapshot of a payment at transaction time.
/// </summary>
public record PaymentSnapshot
{
    public Guid PaymentId { get; init; }
    public string Method { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Note { get; init; }
}

/// <summary>
/// Snapshot of an inventory movement at transaction time.
/// </summary>
public record InventoryMovementSnapshot
{
    public Guid MovementId { get; init; }
    public Guid ProductId { get; init; }
    public int QuantityDelta { get; init; }
    public string Reason { get; init; } = string.Empty;
}
