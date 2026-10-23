using IndyPOS.Application.Common.Constants;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// CloudApi is a resource server: it validates Cloud.Auth's tokens and issues none (spec 2026-10-21, rule 3). It
/// finds Auth's signing keys through Auth's discovery document, so a key Auth rotates is picked up without a deploy.
/// </summary>
public static class AuthTokenValidation
{
    public const string IssuerKey = "Auth:Issuer";

    /// <summary>Cloud.Auth's https launch profile, for a Development run outside Aspire.</summary>
    private const string DevelopmentIssuer = "https://localhost:7190/";

    public static IServiceCollection AddAuthTokenValidation(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var issuer = Issuer(configuration, environment);

        services.AddOpenIddict()
                .AddValidation(options =>
                {
                    // Exactly Auth's issuer, and only tokens meant for CloudApi: Auth also issues tokens for its own
                    // store-client API, which must not pass here.
                    options.SetIssuer(new Uri(issuer, UriKind.Absolute));
                    options.AddAudiences(AuthResources.CloudApi);
                    options.UseSystemNetHttp();
                    options.UseAspNetCore();
                });

        return services;
    }

    private static string Issuer(IConfiguration configuration, IHostEnvironment environment)
    {
        var configured = configuration[IssuerKey];
        if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            return string.IsNullOrEmpty(configured) ? DevelopmentIssuer : configured;

        if (string.IsNullOrEmpty(configured))
            throw new InvalidOperationException(
                $"{IssuerKey} is not set. CloudApi accepts only tokens from Cloud.Auth's issuer, e.g. https://indypos-auth.exconeer.com/.");

        // CloudApi fetches Auth's signing keys from this URL; over http they could be swapped on the way.
        if (!Uri.TryCreate(configured, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"{IssuerKey} must be an https URL; it is '{configured}'.");

        return configured;
    }
}
