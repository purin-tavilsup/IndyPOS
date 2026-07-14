namespace IndyPOS.Application.Abstractions.StoreHub.Services;

/// <summary>
/// Service for managing OAuth2 access tokens for Cloud API communication.
/// Handles token acquisition, caching, and refresh.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Gets a valid access token for Cloud API calls.
    /// Returns cached token if still valid, otherwise fetches new token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Access token, or null if unable to acquire (offline mode)</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the cached token. Call this when receiving 401 from Cloud API.
    /// </summary>
    void ClearCachedToken();
}
