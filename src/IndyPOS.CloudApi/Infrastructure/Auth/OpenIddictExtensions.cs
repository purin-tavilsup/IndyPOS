using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// OpenIddict configuration for OAuth2 Client Credentials flow.
/// Stores authenticate using their ClientId/ClientSecret to get JWT access tokens.
/// </summary>
public static class OpenIddictExtensions
{
    public static IServiceCollection AddOpenIddictServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                // Use EF Core for storing OpenIddict entities (applications, tokens, etc.)
                options.UseEntityFrameworkCore()
                       .UseDbContext<CloudDbContext>();
            })
            .AddServer(options =>
            {
                // Enable Client Credentials flow for machine-to-machine auth
                options.AllowClientCredentialsFlow();

                // Token endpoint for store authentication
                options.SetTokenEndpointUris("/oauth/token");

                // Token lifetimes per security spec
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
                options.SetRefreshTokenLifetime(TimeSpan.FromHours(24));

                // Register scopes for authorization
                options.RegisterScopes(
                    "sync.write",   // Upload events to cloud
                    "master.read"   // Download master data
                );

                // Development: use ephemeral signing keys (auto-generated)
                // Production: use AddSigningKey() with RSA key from secrets manager
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // Disable encryption for access tokens (simpler JWTs)
                options.DisableAccessTokenEncryption();

                // ASP.NET Core integration
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                // Use local server for token validation
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
