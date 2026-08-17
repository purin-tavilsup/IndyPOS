namespace IndyPOS.MigrationTool.Services;

/// <summary>
/// The one definition of what a legacy barcode looks like once stored in v4.
/// </summary>
/// <remarks>
/// <para>
/// <c>Product.Barcode</c> is capped at 50 characters, and real stores hold barcodes longer than
/// that: a scanned TISI certification QR code lands a 90-character URL in the field. Two such rows
/// exist in GeneralHardware and one in MimyMart.
/// </para>
/// <para>
/// It lives here, shared, because the barcode is also the JOIN KEY the verifier compares on. When
/// the migrator truncated and the verifier did not, every overlong row looked like a product that
/// had failed to migrate -- <c>verify</c> exited 1 on two of the three real stores while the data
/// was perfectly correct. Keeping one definition is what stops the write and the read drifting
/// apart again.
/// </para>
/// </remarks>
public static class LegacyBarcode
{
    /// <summary>Matches <c>ProductConfiguration</c>'s <c>HasMaxLength(50)</c> on the barcode.</summary>
    public const int MaxLength = 50;

    /// <returns>The barcode as it is stored in v4: never longer than <see cref="MaxLength"/>.</returns>
    public static string ToStored(string? legacyBarcode)
    {
        var barcode = legacyBarcode ?? string.Empty;

        return barcode.Length <= MaxLength ? barcode : barcode[..MaxLength];
    }
}
