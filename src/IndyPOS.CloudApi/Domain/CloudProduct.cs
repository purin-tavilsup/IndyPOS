namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// Master product managed in Cloud.
/// Distributed to stores via GET /master/products.
/// </summary>
public class CloudProduct
{
    public Guid Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Manufacturer { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? GroupPrice { get; set; }
    public int? GroupPriceQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
}
