namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core payment entity for StoreHub.
/// Method is stored as string (no lookup table needed).
/// </summary>
public class Payment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Method { get; set; } = default!;  // Cash, Card, PayLater, etc.
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedUtc { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = default!;
    public PayLater? PayLater { get; set; }
}
