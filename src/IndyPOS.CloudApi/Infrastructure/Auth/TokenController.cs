using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// OAuth2 token endpoint for Client Credentials flow.
/// Stores exchange their ClientId/ClientSecret for JWT access tokens.
/// </summary>
[ApiController]
public class TokenController : ControllerBase
{
    private readonly CloudDbContext _dbContext;
    private readonly ILogger<TokenController> _logger;

    public TokenController(CloudDbContext dbContext, ILogger<TokenController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("~/oauth/token")]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (!request.IsClientCredentialsGrantType())
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.UnsupportedGrantType,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Only client_credentials grant type is supported."
                    }));
        }

        var clientId = request.ClientId;
        var clientSecret = request.ClientSecret;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Client credentials are required."
                    }));
        }

        // Find store config by ClientId
        var storeConfig = await _dbContext.StoreConfigs
            .FirstOrDefaultAsync(s => s.ClientId == clientId);

        if (storeConfig is null || !storeConfig.IsActive)
        {
            _logger.LogWarning("Token request for unknown or inactive client: {ClientId}", clientId);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified client credentials are invalid."
                    }));
        }

        // Verify secret using BCrypt
        if (string.IsNullOrEmpty(storeConfig.ClientSecretHash) ||
            !BCrypt.Net.BCrypt.Verify(clientSecret, storeConfig.ClientSecretHash))
        {
            _logger.LogWarning("Invalid client secret for store: {StoreId}", storeConfig.StoreId);
            return Forbid(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new Microsoft.AspNetCore.Authentication.AuthenticationProperties(
                    new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.InvalidClient,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The specified client credentials are invalid."
                    }));
        }

        // Update last authenticated timestamp
        storeConfig.LastAuthenticatedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Token issued for store: {StoreId}", storeConfig.StoreId);

        // Create claims identity with store info
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(Claims.Subject, storeConfig.StoreId);
        identity.AddClaim(Claims.Name, storeConfig.StoreName);
        identity.AddClaim("store_id", storeConfig.StoreId);

        // Set destinations for claims (access token only, no ID token for client credentials)
        identity.SetDestinations(static claim => claim.Type switch
        {
            Claims.Name or Claims.Subject or "store_id"
                => [Destinations.AccessToken],
            _ => []
        });

        // Add requested scopes
        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }
}
