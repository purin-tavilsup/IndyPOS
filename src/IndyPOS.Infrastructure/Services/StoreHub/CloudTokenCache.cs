namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>The store's cloud access token, shared by every CloudTokenService in the process (a singleton).</summary>
public sealed class CloudTokenCache
{
    public SemaphoreSlim Gate { get; } = new(1, 1);
    public string? Token { get; set; }
    public DateTime ExpiresAtUtc { get; set; } = DateTime.MinValue;
}
