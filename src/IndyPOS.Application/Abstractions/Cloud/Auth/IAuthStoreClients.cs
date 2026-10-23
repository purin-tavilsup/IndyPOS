namespace IndyPOS.Application.Abstractions.Cloud.Auth;

public sealed record StoreClientCredentials(string ClientId, string ClientSecret);

/// <summary>Cloud.Auth could not be reached, or refused a store-client request.</summary>
public sealed class AuthUnavailableException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// The stores' clients, kept by Cloud.Auth: CloudApi creates one when it registers a store, and deletes it again if
/// its own registration then fails. Auth is the only issuer, so the secret lives only there.
/// </summary>
public interface IAuthStoreClients
{
    /// <exception cref="AuthUnavailableException">Auth was down or refused.</exception>
    Task<StoreClientCredentials> CreateAsync(string storeId, string displayName, CancellationToken cancellationToken = default);

    /// <exception cref="AuthUnavailableException">Auth was down or refused.</exception>
    Task DeleteAsync(string storeId, CancellationToken cancellationToken = default);

    /// <exception cref="AuthUnavailableException">Auth was down or refused.</exception>
    Task DisableAsync(string storeId, CancellationToken cancellationToken = default);
}
