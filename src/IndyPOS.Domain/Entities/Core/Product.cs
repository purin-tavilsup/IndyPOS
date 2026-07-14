namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core product entity for StoreHub.
/// No QuantityInStock - use InventoryMovement for stock tracking.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public string Barcode { get; set; } = default!; 
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? GroupPrice { get; set; }
    public int? GroupPriceQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    // Navigation properties
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
