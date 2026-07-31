using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Common.Models;

[ExcludeFromCodeCoverage]
public class Product
{
    /// <summary>
    /// Primary product ID (StoreHub UUID).
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Legacy SQLite product ID. Zero for StoreHub-only products.
    /// </summary>
    [Obsolete("Use Id (Guid) instead. Kept for legacy SQLite compatibility.")]
    public int InventoryProductId { get; init; }

    public string Barcode { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string Brand { get; init; } = string.Empty;

    /// <summary>Catalogue category code (see ProductCategoryCodes), not a legacy numeric id.</summary>
    public string Category { get; init; } = string.Empty;

    public int Quantity { get; set; }

    public int? GroupPriceQuantity { get; init; }

    public int Priority { get; set; }

    public bool IsTrackable { get; init; }

    public string Note { get; set; } = string.Empty;
    
    public decimal UnitPrice { get; set; }
    
    public decimal GroupPrice { get; set; }
    
    public bool IsGroupProduct { get; set; }
    
    public decimal OriginalUnitPrice { get; set; }

    /// <summary>
    /// Calculates the total price for this product line.
    /// Uses GroupPrice if IsGroupProduct, otherwise UnitPrice * Quantity.
    /// </summary>
    public decimal GetTotal() => IsGroupProduct ? GroupPrice : UnitPrice * Quantity;
}