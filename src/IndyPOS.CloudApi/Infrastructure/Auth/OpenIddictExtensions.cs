using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// OpenIddict configuration for OAuth2 Client Credentials flow.
/// Stores authenticate using their ClientId/ClientSecret to get JWT access tokens.
/// </summary>
public static class OpenIddictExtensions
{
    /// <summary>
    /// Environment variable name for RSA private key (PEM format, base64-encoded).
    /// </summary>
    public const string RsaSigningKeyEnvVar = "INDYPOS_RSA_SIGNING_KEY";

    /// <summary>
    /// Configuration key declaring that a reverse proxy terminates TLS in front of this host, so the
    /// container itself only ever receives plain HTTP over an internal network.
    /// </summary>
    public const string TlsTerminatedUpstreamKey = "OpenIddict:TlsTerminatedUpstream";

    public static IServiceCollection AddOpenIddictServer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Try to load RSA key from environment variable (production)
        var rsaKeyBase64 = Environment.GetEnvironmentVariable(RsaSigningKeyEnvVar)
            ?? configuration["OpenIddict:RsaSigningKey"];

        // Defaults to false: strict transport security unless a deployment explicitly declares
        // otherwise. An unconfigured host refuses to issue tokens over cleartext rather than doing
        // it silently.
        var tlsTerminatedUpstream = configuration.GetValue<bool>(TlsTerminatedUpstreamKey);

        RsaSecurityKey? rsaSecurityKey = null;
        if (!string.IsNullOrEmpty(rsaKeyBase64))
        {
            var rsa = LoadRsaKeyFromBase64(rsaKeyBase64);
            rsaSecurityKey = new RsaSecurityKey(rsa)
            {
                KeyId = GenerateKeyId(rsa)
            };
        }

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

                // Configure signing and encryption keys
                if (rsaSecurityKey is not null)
                {
                    // Production: use RSA key from environment
                    options.AddSigningKey(rsaSecurityKey);
                    options.AddEncryptionKey(rsaSecurityKey);
                }
                else
                {
                    // Development: use ephemeral signing keys (auto-generated on startup)
                    // WARNING: Tokens become invalid on app restart
                    options.AddDevelopmentEncryptionCertificate()
                           .AddDevelopmentSigningCertificate();
                }

                // Disable encryption for access tokens (simpler JWTs)
                options.DisableAccessTokenEncryption();

                // ASP.NET Core integration
                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough();

                // OpenIddict rejects token requests that did not arrive over HTTPS (error ID2083).
                // In the containerised deployment TLS terminates at a reverse proxy, so this host
                // sees plain HTTP on an internal network the public cannot reach — the store-to-cloud
                // leg is still HTTPS, because StoreHub's CloudApi:BaseUrl points at the proxy.
                //
                // This stays OFF by default. Turning it on without a terminator in front means
                // tokens are issued over cleartext.
                if (tlsTerminatedUpstream)
                {
                    options.UseAspNetCore()
                           .DisableTransportSecurityRequirement();
                }
            })
            .AddValidation(options =>
            {
                // Use local server for token validation
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }

    /// <summary>
    /// Load RSA key from base64-encoded PEM string.
    /// Supports both PKCS#8 (BEGIN PRIVATE KEY) and PKCS#1 (BEGIN RSA PRIVATE KEY) formats.
    /// </summary>
    private static RSA LoadRsaKeyFromBase64(string base64Key)
    {
        // Decode base64 to PEM string
        var pemBytes = Convert.FromBase64String(base64Key);
        var pemString = System.Text.Encoding.UTF8.GetString(pemBytes);

        var rsa = RSA.Create();

        // Determine PEM format and import accordingly
        if (pemString.Contains("BEGIN PRIVATE KEY"))
        {
            // PKCS#8 format
            rsa.ImportFromPem(pemString);
        }
        else if (pemString.Contains("BEGIN RSA PRIVATE KEY"))
        {
            // PKCS#1 format
            rsa.ImportFromPem(pemString);
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid RSA key format in {RsaSigningKeyEnvVar}. " +
                "Expected PEM format (PKCS#1 or PKCS#8), base64-encoded.");
        }

        // Validate key size (minimum 2048 bits for security)
        if (rsa.KeySize < 2048)
        {
            throw new InvalidOperationException(
                $"RSA key size must be at least 2048 bits. Current: {rsa.KeySize} bits.");
        }

        return rsa;
    }

    /// <summary>
    /// Generate a stable key ID from RSA public key for key rotation support.
    /// Uses first 8 chars of SHA256 hash of the public key.
    /// </summary>
    private static string GenerateKeyId(RSA rsa)
    {
        var publicKeyBytes = rsa.ExportRSAPublicKey();
        var hash = SHA256.HashData(publicKeyBytes);
        return Convert.ToHexString(hash)[..8].ToLowerInvariant();
    }
}
