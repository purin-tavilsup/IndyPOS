using System.Security.Cryptography;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Cloud.Auth.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;

namespace IndyPOS.Cloud.Auth.OpenIddict;

/// <summary>
/// OpenIddict as the cloud's one token issuer. Moved from CloudApi with two changes: outside Development the signing
/// key must be configured (a generated key would invalidate every token on each redeploy), and the issuer is
/// configured, ending in "/", so it matches exactly what CloudApi validates.
/// </summary>
internal static class AuthServerSetup
{
    public const string RsaSigningKeyEnvVar = "INDYPOS_RSA_SIGNING_KEY";
    public const string TlsTerminatedUpstreamKey = "OpenIddict:TlsTerminatedUpstream";
    public const string IssuerKey = "OpenIddict:Issuer";

    public static IServiceCollection AddAuthServer(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var signingKey = LoadSigningKey(configuration, environment);
        var tlsTerminatedUpstream = configuration.GetValue<bool>(TlsTerminatedUpstreamKey);
        var issuer = Issuer(configuration, environment);

        services.AddOpenIddict()
                .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<AuthDbContext>())
                .AddServer(options =>
                {
                    options.AllowClientCredentialsFlow();
                    options.SetTokenEndpointUris("/oauth/token");
                    options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
                    options.RegisterScopes(AuthScopes.SyncWrite, AuthScopes.MasterRead, AuthScopes.StoreClients);

                    if (!string.IsNullOrEmpty(issuer))
                        options.SetIssuer(new Uri(issuer, UriKind.Absolute));

                    if (signingKey is not null)
                        options.AddSigningKey(signingKey).AddEncryptionKey(signingKey);
                    else
                        options.AddDevelopmentEncryptionCertificate().AddDevelopmentSigningCertificate();

                    // Simpler JWTs that CloudApi validates with the signing key alone.
                    options.DisableAccessTokenEncryption();

                    var aspNetCore = options.UseAspNetCore().EnableTokenEndpointPassthrough();

                    // TLS terminates at Caddy; this host sees plain http on a network the public cannot reach.
                    // Off by default: turning it on with no terminator in front would issue tokens over cleartext.
                    if (tlsTerminatedUpstream)
                        aspNetCore.DisableTransportSecurityRequirement();
                })
                .AddValidation(options =>
                {
                    // Auth's own API (store clients) accepts only tokens for Auth: a store token is not even
                    // authenticated there, whatever scope a mistake might one day grant it.
                    options.UseLocalServer();
                    options.AddAudiences(AuthResources.Auth);
                    options.UseAspNetCore();
                });

        if (tlsTerminatedUpstream)
            services.Configure<ForwardedHeadersOptions>(ConfigureForwardedHeaders);

        return services;
    }

    public static WebApplication UseAuthForwardedHeaders(this WebApplication app)
    {
        // Behind Caddy the scheme comes from X-Forwarded-Proto, so the issuer and endpoints advertise https.
        if (app.Configuration.GetValue<bool>(TlsTerminatedUpstreamKey))
            app.UseForwardedHeaders();

        return app;
    }

    private static void ConfigureForwardedHeaders(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

        // Caddy is on the private compose network; only it can reach this container.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }

    /// <remarks>
    /// Outside Development the issuer is required and https: without it, the issuer would come from each request's
    /// Host and CloudApi, which pins one, would refuse every token with no clear reason; over http, CloudApi would
    /// fetch the signing keys in the clear.
    /// </remarks>
    private static string? Issuer(IConfiguration configuration, IHostEnvironment environment)
    {
        var issuer = configuration[IssuerKey];
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            return issuer;

        if (string.IsNullOrEmpty(issuer) || !Uri.TryCreate(issuer, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException(
                $"{IssuerKey} must be Auth's public https URL ending in '/', e.g. https://indypos-auth.exconeer.com/. It is '{issuer}'.");

        return issuer;
    }

    private static RsaSecurityKey? LoadSigningKey(IConfiguration configuration, IHostEnvironment environment)
    {
        var base64 = Environment.GetEnvironmentVariable(RsaSigningKeyEnvVar) ?? configuration["OpenIddict:RsaSigningKey"];

        if (string.IsNullOrEmpty(base64))
        {
            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
                return null;

            throw new InvalidOperationException(
                $"No signing key configured. Set {RsaSigningKeyEnvVar} (base64 PEM, RSA 2048+). Outside Development a generated " +
                "key would make every token invalid after each redeploy.");
        }

        var rsa = RsaKeys.FromBase64Pem(base64, RsaSigningKeyEnvVar);
        return new RsaSecurityKey(rsa) { KeyId = RsaKeys.KeyId(rsa) };
    }
}

/// <summary>RSA key loading, moved as is from CloudApi.</summary>
internal static class RsaKeys
{
    public static RSA FromBase64Pem(string base64Key, string settingName)
    {
        var pem = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(base64Key));

        if (!pem.Contains("BEGIN PRIVATE KEY") && !pem.Contains("BEGIN RSA PRIVATE KEY"))
            throw new InvalidOperationException($"Invalid RSA key format in {settingName}. Expected PEM (PKCS#1 or PKCS#8), base64-encoded.");

        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);

        if (rsa.KeySize < 2048)
            throw new InvalidOperationException($"RSA key size must be at least 2048 bits. Current: {rsa.KeySize} bits.");

        return rsa;
    }

    /// <summary>A stable key id from the public key, for rotation: the first 8 hex characters of its SHA-256.</summary>
    public static string KeyId(RSA rsa) =>
        Convert.ToHexString(SHA256.HashData(rsa.ExportRSAPublicKey()))[..8].ToLowerInvariant();
}
