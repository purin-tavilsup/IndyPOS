using System.Security.Claims;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Cloud.Auth.Data;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.Cloud.Auth.Tokens;

/// <summary>
/// POST /oauth/token, client credentials only. OpenIddict has already checked the client's secret and permissions;
/// this decides who the token is for. A store gets one only while Auth has it enabled, and its token is for CloudApi.
/// </summary>
internal static class TokenEndpoint
{
    public static IEndpointRouteBuilder MapTokenEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/oauth/token", IssueAsync);
        return app;
    }

    private static async Task<IResult> IssueAsync(HttpContext context, AuthDbContext db, ILoggerFactory loggerFactory)
    {
        var request = context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (!request.IsClientCredentialsGrantType())
            return Refuse(Errors.UnsupportedGrantType, "Only the client_credentials grant is supported.");

        var store = await db.StoreClients.SingleOrDefaultAsync(s => s.ClientId == request.ClientId, context.RequestAborted);
        if (store is null || !store.Enabled)
        {
            loggerFactory.CreateLogger(nameof(TokenEndpoint)).LogWarning("Token refused for unknown or disabled client {ClientId}", request.ClientId);
            return Refuse(Errors.InvalidClient, "The specified client credentials are invalid.");
        }

        store.LastAuthenticatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(context.RequestAborted);

        return Results.SignIn(StorePrincipal(store.StoreId, request), authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static ClaimsPrincipal StorePrincipal(string storeId, OpenIddictRequest request)
    {
        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, Claims.Name, Claims.Role);
        identity.AddClaim(Claims.Subject, storeId);
        identity.AddClaim(Claims.Name, storeId);
        identity.AddClaim("store_id", storeId);
        identity.SetDestinations(static _ => [Destinations.AccessToken]);

        var principal = new ClaimsPrincipal(identity);
        principal.SetScopes(request.GetScopes());
        principal.SetResources(AuthResources.CloudApi);
        return principal;
    }

    private static IResult Refuse(string error, string description) =>
        Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            }),
            [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
}
