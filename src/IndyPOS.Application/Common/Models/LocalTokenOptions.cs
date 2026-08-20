namespace IndyPOS.Application.Common.Models;

/// <summary>
/// Configuration options for local JWT token generation.
/// Used by both StoreHub (token generation) and CloudApi (token validation).
/// </summary>
public class LocalTokenOptions
{
    public const string SectionName = "LocalToken";

    /// <summary>
    /// The key this class falls back to when no LocalToken section is configured. It is published in
    /// a public repository, so it is a placeholder for local development only — never a usable
    /// secret. <see cref="LocalTokenOptionsValidator"/> refuses to start a host that still has it.
    /// </summary>
    public const string BuiltInDefaultSecretKey = "IndyPOS-StoreHub-Local-Auth-Secret-Key-2026";

    /// <summary>
    /// Secret key for signing tokens. Must be at least 32 characters.
    /// </summary>
    public string SecretKey { get; set; } = BuiltInDefaultSecretKey;

    /// <summary>
    /// True when this instance carries no usable secret — either the built-in default or nothing at
    /// all. Both cases mean "the operator has not supplied a key".
    /// </summary>
    public bool UsesBuiltInDefaultSecretKey =>
        string.IsNullOrWhiteSpace(SecretKey) || SecretKey == BuiltInDefaultSecretKey;

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
