using System.Text.Json;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace McpSignIn;

/// <summary>
/// Client ID Metadata Document support, which OpenIddict does not have (openiddict-core #2416 was
/// declined). When a request's client_id is an https URL we have not seen, fetch the document it
/// points at, check it, and register it as a public, PKCE-only client before OpenIddict looks it up.
/// Spike-grade: no SSRF guard beyond https-only, no re-fetch when a document changes.
/// </summary>
public sealed class CimdClientRegistrar(
    IOpenIddictApplicationManager applications,
    IHttpClientFactory httpClients,
    ILogger<CimdClientRegistrar> logger)
    : IOpenIddictServerHandler<OpenIddictServerEvents.ExtractAuthorizationRequestContext>,
      IOpenIddictServerHandler<OpenIddictServerEvents.ExtractTokenRequestContext>
{
    public const string HttpClientName = "cimd";

    public ValueTask HandleAsync(OpenIddictServerEvents.ExtractAuthorizationRequestContext context) =>
        EnsureRegisteredAsync(context.Request?.ClientId, context.CancellationToken);

    public ValueTask HandleAsync(OpenIddictServerEvents.ExtractTokenRequestContext context) =>
        EnsureRegisteredAsync(context.Request?.ClientId, context.CancellationToken);

    private async ValueTask EnsureRegisteredAsync(string? clientId, CancellationToken cancellationToken)
    {
        if (!IsMetadataDocumentUrl(clientId, out var documentUrl))
        {
            logger.LogInformation("client_id {ClientId} is not a CIMD URL; leaving it to OpenIddict", clientId);
            return;
        }

        if (await applications.FindByClientIdAsync(clientId!, cancellationToken) is not null)
        {
            return;
        }

        var document = await FetchDocumentAsync(documentUrl, cancellationToken);
        if (document is null)
        {
            return;
        }

        await applications.CreateAsync(ToDescriptor(clientId!, document.Value), cancellationToken);
        logger.LogWarning("CIMD client registered: {ClientId}", clientId);
    }

    private static bool IsMetadataDocumentUrl(string? clientId, out Uri documentUrl)
    {
        documentUrl = null!;
        return Uri.TryCreate(clientId, UriKind.Absolute, out documentUrl!)
               && documentUrl.Scheme == Uri.UriSchemeHttps
               && documentUrl.AbsolutePath.Length > 1;
    }

    private async Task<JsonElement?> FetchDocumentAsync(Uri documentUrl, CancellationToken cancellationToken)
    {
        try
        {
            var json = await httpClients.CreateClient(HttpClientName)
                                        .GetStringAsync(documentUrl, cancellationToken);
            logger.LogWarning("CIMD document from {Url}: {Json}", documentUrl, json);

            var document = JsonDocument.Parse(json).RootElement;
            return IsValid(document, documentUrl) ? document : null;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException)
        {
            logger.LogError(exception, "CIMD document fetch failed for {Url}", documentUrl);
            return null;
        }
    }

    private bool IsValid(JsonElement document, Uri documentUrl)
    {
        var claimedId = document.TryGetProperty("client_id", out var id) ? id.GetString() : null;
        if (claimedId != documentUrl.OriginalString)
        {
            logger.LogError("CIMD client_id {Claimed} does not match its URL {Url}", claimedId, documentUrl);
            return false;
        }

        var authMethod = document.TryGetProperty("token_endpoint_auth_method", out var method)
            ? method.GetString()
            : ClientAuthenticationMethods.None;
        if (authMethod != ClientAuthenticationMethods.None)
        {
            logger.LogError("CIMD client wants {Method}; only 'none' (public + PKCE) is accepted", authMethod);
            return false;
        }

        return document.TryGetProperty("redirect_uris", out var uris) && uris.GetArrayLength() > 0;
    }

    private static OpenIddictApplicationDescriptor ToDescriptor(string clientId, JsonElement document)
    {
        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientType = ClientTypes.Public,
            DisplayName = document.TryGetProperty("client_name", out var name) ? name.GetString() : clientId,
            Permissions =
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.GrantTypes.RefreshToken,
                Permissions.ResponseTypes.Code,
            },
            Requirements = { Requirements.Features.ProofKeyForCodeExchange },
        };

        foreach (var uri in document.GetProperty("redirect_uris").EnumerateArray())
        {
            descriptor.RedirectUris.Add(new Uri(uri.GetString()!));
        }

        return descriptor;
    }
}
