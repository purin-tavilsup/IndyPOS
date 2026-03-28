namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// Cloud-side invoice entity.
/// Materialized from InvoiceCompletedEvent.
/// </summary>
public class CloudInvoice
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }

    // Navigation properties
    public ICollection<CloudInvoiceLine> Lines { get; set; } = new List<CloudInvoiceLine>();
    public ICollection<CloudPayment> Payments { get; set; } = new List<CloudPayment>();
}

/// <summary>
/// Cloud-side invoice line entity.
/// </summary>
public class CloudInvoiceLine
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigation
    public CloudInvoice Invoice { get; set; } = default!;
}

/// <summary>
/// Cloud-side payment entity.
/// </summary>
public class CloudPayment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Method { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }

    // Navigation
    public CloudInvoice Invoice { get; set; } = default!;
}

/// <summary>
/// Cloud-side inventory movement entity.
/// </summary>
public class CloudInventoryMovement
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public int QuantityDelta { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}
