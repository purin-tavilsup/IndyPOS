namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core invoice entity for StoreHub.
/// Simplified design - no CustomerId, no status (all completed on save).
/// </summary>
public class Invoice
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    // Navigation properties
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
