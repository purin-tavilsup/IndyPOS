namespace IndyPOS.Application.Abstractions.Cloud.Auth;

/// <summary>
/// Creates the OAuth2 client a store authenticates as. The store's public metadata lives in
/// CloudStoreConfig; the client secret lives here, owned by OpenIddict's application store.
/// Registration writes both in one transaction so neither can exist without the other.
/// </summary>
public interface IStoreClientCredentialStore
{
    Task CreateAsync(
        string clientId,
        string clientSecret,
        string displayName,
        CancellationToken cancellationToken = default);
}
