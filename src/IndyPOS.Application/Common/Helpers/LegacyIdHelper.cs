namespace IndyPOS.Application.Common.Helpers;

/// <summary>
/// Helper for converting between legacy SQLite int IDs and Guids.
/// Uses a deterministic mapping to ensure consistency.
/// </summary>
public static class LegacyIdHelper
{
    // Fixed namespace bytes for InventoryProduct IDs: "IndyPOS-Prod"
    private static readonly byte[] NamespaceBytes = { 0x49, 0x6E, 0x64, 0x79, 0x50, 0x4F, 0x53, 0x2D, 0x50, 0x72, 0x6F, 0x64 };

    /// <summary>
    /// Converts a legacy SQLite int ID to a deterministic Guid.
    /// The int is stored in the last 4 bytes of the Guid.
    /// </summary>
    public static Guid ToGuid(int legacyId)
    {
        var idBytes = BitConverter.GetBytes(legacyId);
        var combined = new byte[16];
        Array.Copy(NamespaceBytes, 0, combined, 0, 12);
        Array.Copy(idBytes, 0, combined, 12, 4);
        return new Guid(combined);
    }

    /// <summary>
    /// Extracts the legacy int ID from a deterministic Guid.
    /// The int is stored in the last 4 bytes of the Guid.
    /// </summary>
    public static int ToInt(Guid id)
    {
        var bytes = id.ToByteArray();
        return BitConverter.ToInt32(bytes, 12);
    }
}
