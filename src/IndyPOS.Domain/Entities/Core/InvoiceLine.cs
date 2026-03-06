namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core invoice line entity for StoreHub.
/// Simplified design (7 fields). ProductName is a snapshot for historical accuracy.
/// </summary>
public class InvoiceLine
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedUtc { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = default!;
    public Product Product { get; set; } = default!;

    // Calculated property (no DB storage)
    public decimal LineTotal => UnitPrice * Quantity;
}
