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

    /// <summary>
    /// The legacy SQLite InventoryProductId this row was migrated from, or <c>null</c> for a row v4 created
    /// itself. Defect 8: without it a migrated row cannot be reconciled against its source, and for
    /// invoices there is no natural key to fall back on.
    /// </summary>
    /// <remarks>
    /// Nullable rather than 0-when-absent, unlike <c>StoreUser.LegacyUserId</c> which predates this:
    /// a row v4 created has no legacy id, and 0 is a magic value standing in for absence. Nullable
    /// also keeps the unique index honest, because PostgreSQL treats NULLs as distinct.
    /// </remarks>
    public int? LegacyProductId { get; set; }

    // Navigation properties
    public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
    public ICollection<InventoryMovement> InventoryMovements { get; set; } = new List<InventoryMovement>();
}
