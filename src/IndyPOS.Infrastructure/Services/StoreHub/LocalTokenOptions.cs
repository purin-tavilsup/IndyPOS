namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Configuration options for local JWT token generation.
/// </summary>
public class LocalTokenOptions
{
    public const string SectionName = "LocalToken";

    /// <summary>
    /// Secret key for signing tokens. Must be at least 32 characters.
    /// </summary>
    public string SecretKey { get; set; } = "IndyPOS-StoreHub-Local-Auth-Secret-Key-2026";

    /// <summary>
    /// Token issuer.
    /// </summary>
    public string Issuer { get; set; } = "IndyPOS.StoreHub";

    /// <summary>
    /// Token audience.
    /// </summary>
    public string Audience { get; set; } = "IndyPOS.POS";

    /// <summary>
    /// Token expiry in hours. Default 12 hours (covers long shifts).
    /// </summary>
    public int ExpiryHours { get; set; } = 12;
}
