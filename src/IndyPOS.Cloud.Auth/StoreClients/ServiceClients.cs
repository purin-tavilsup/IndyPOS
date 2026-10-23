using IndyPOS.Application.Common.Constants;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.Cloud.Auth.StoreClients;

/// <summary>
/// The confidential clients Auth's own APIs trust. Today one: CloudApi, which creates and disables store clients. Its
/// secret comes from configuration in every environment and is applied at start, so a changed secret takes effect on
/// the next restart without touching the database by hand.
/// </summary>
internal static class ServiceClients
{
    public const string CloudApiClientId = AuthServiceClients.CloudApiClientId;
    public const string CloudApiSecretKey = AuthServiceClients.CloudApiSecretKey;

    /// <summary>Fails fast: without the secret CloudApi could never register a store.</summary>
    public static string CloudApiSecret(IConfiguration configuration) =>
        configuration[CloudApiSecretKey] is { Length: > 0 } secret
            ? secret
            : throw new InvalidOperationException($"{CloudApiSecretKey} is not set. CloudApi needs it to register stores in Auth.");
}

/// <summary>Creates or updates the service clients when Auth starts.</summary>
internal sealed class ServiceClientSeeder(IServiceProvider services, IConfiguration configuration) : IHostedService
{
    private readonly string _cloudApiSecret = ServiceClients.CloudApiSecret(configuration);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = ServiceClients.CloudApiClientId,
            ClientSecret = _cloudApiSecret,
            ClientType = ClientTypes.Confidential,
            DisplayName = "IndyPOS CloudApi",
            Permissions =
            {
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.ClientCredentials,
                Permissions.Prefixes.Scope + AuthScopes.StoreClients
            }
        };

        var existing = await applications.FindByClientIdAsync(ServiceClients.CloudApiClientId, cancellationToken);
        if (existing is null)
            await applications.CreateAsync(descriptor, cancellationToken);
        else
            await applications.UpdateAsync(existing, descriptor, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
