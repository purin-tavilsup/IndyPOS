using IndyPOS.Application.Abstractions.Cloud.Auth;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Creates the OpenIddict application row a store authenticates as. The manager hashes the
/// secret; only the hash is persisted. The permission entries are load-bearing — without them
/// OpenIddict's endpoint/grant/scope validation handlers reject the request even though the
/// client exists.
/// </summary>
public sealed class OpenIddictStoreClientCredentialStore : IStoreClientCredentialStore
{
    private readonly IOpenIddictApplicationManager _applicationManager;

    public OpenIddictStoreClientCredentialStore(IOpenIddictApplicationManager applicationManager)
    {
        _applicationManager = applicationManager;
    }

    public async Task CreateAsync(
        string clientId,
        string clientSecret,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            DisplayName = displayName,
            ClientType = ClientTypes.Confidential,
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + "sync.write",
                Permissions.Prefixes.Scope + "master.read"
            }
        };

        await _applicationManager.CreateAsync(descriptor, cancellationToken);
    }
}
