using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace McpSignIn;

public static class AuthorizeEndpoint
{
    /// <summary>
    /// OpenIddict validated the request (client, redirect, PKCE) before we get here. We sign the
    /// person in with Google, check they are allowed, and hand OpenIddict the identity to put in
    /// the code. In the real Auth service "allowed" means an Active account; here it is a list.
    /// </summary>
    public static void MapAuthorizeEndpoint(this WebApplication app, string mcpResource) =>
        app.MapGet("connect/authorize", async (HttpContext http, IConfiguration configuration) =>
        {
            var request = http.GetOpenIddictServerRequest()
                          ?? throw new InvalidOperationException("Not an OpenID Connect request.");

            var cookie = await http.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            if (!cookie.Succeeded)
            {
                var returnUrl = http.Request.PathBase + http.Request.Path + http.Request.QueryString;
                return Results.Challenge(new AuthenticationProperties { RedirectUri = returnUrl },
                                         [GoogleDefaults.AuthenticationScheme]);
            }

            var email = cookie.Principal.FindFirstValue(ClaimTypes.Email);
            if (!IsAllowed(configuration, email))
            {
                return Results.Forbid(Refusal($"{email} is not allowed to sign in."),
                                      [OpenIddictServerAspNetCoreDefaults.AuthenticationScheme]);
            }

            var identity = new ClaimsIdentity(TokenValidationParameters.DefaultAuthenticationType,
                                              Claims.Name, Claims.Role);
            identity.SetClaim(Claims.Subject, cookie.Principal.FindFirstValue(ClaimTypes.NameIdentifier))
                    .SetClaim(Claims.Email, email)
                    .SetClaim(Claims.Name, cookie.Principal.FindFirstValue(ClaimTypes.Name));
            identity.SetScopes(request.GetScopes());
            identity.SetResources(request.GetResources() is { IsDefaultOrEmpty: false } resources
                ? resources
                : [mcpResource]);
            identity.SetDestinations(_ => [Destinations.AccessToken]);

            app.Logger.LogWarning("Signed in {Email} for client {ClientId}, scopes [{Scopes}], resources [{Resources}]",
                                  email, request.ClientId, string.Join(' ', request.GetScopes()),
                                  string.Join(' ', request.GetResources()));

            return Results.SignIn(new ClaimsPrincipal(identity), properties: null,
                                  OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        });

    private static bool IsAllowed(IConfiguration configuration, string? email) =>
        email is not null
        && (configuration["Spike:AllowedEmails"] ?? string.Empty)
           .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Contains(email, StringComparer.OrdinalIgnoreCase);

    private static AuthenticationProperties Refusal(string description) => new(new Dictionary<string, string?>
    {
        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description,
    });
}
