namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Movement-based inventory tracking.
/// Replaces snapshot-based QuantityInStock for accurate historical tracking.
/// Current stock = SUM(QuantityDelta) WHERE StoreId AND ProductId.
/// </summary>
public class InventoryMovement
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public Guid ProductId { get; set; }
    public int QuantityDelta { get; set; }  // Positive = increase, Negative = decrease
    public string Reason { get; set; } = default!;  // Sale, Restock, Adjustment, TransferIn, TransferOut, Loss, Return
    public Guid? ReferenceId { get; set; }  // InvoiceId for sales, etc.
    public string? Note { get; set; }

    /// <summary>
    /// Who moved the stock: the seller for a sale, the caller for an adjustment. Null for rows written
    /// before the column existed, and for movements no signed-in user makes (initial stock on product
    /// creation, migrated v3 history).
    /// </summary>
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedUtc { get; set; }

    // Navigation properties
    public Product Product { get; set; } = default!;
}
