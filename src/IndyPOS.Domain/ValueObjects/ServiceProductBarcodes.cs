namespace IndyPOS.Domain.ValueObjects;

/// <summary>
/// The service products sold from the sale panel's quick buttons. The barcodes are MimyShop's real
/// ones (its v3 till pinned them), so every MimyShop till and migration must keep them verbatim.
/// </summary>
public static class ServiceProductBarcodes
{
    public const string Delivery = "2002500000014";   // จัดส่ง
    public const string Documents = "2002500000021";  // เอกสาร

    public static IReadOnlyList<string> All { get; } = [Delivery, Documents];
}
