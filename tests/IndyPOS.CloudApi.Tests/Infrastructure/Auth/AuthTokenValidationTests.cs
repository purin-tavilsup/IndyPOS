using FluentAssertions;
using IndyPOS.CloudApi.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Xunit;

namespace IndyPOS.CloudApi.Tests.Infrastructure.Auth;

/// <summary>
/// CloudApi trusts exactly one issuer and fetches its keys from it, so outside Development the issuer must be set and
/// be https: over http the keys could be swapped on the way.
/// </summary>
public class AuthTokenValidationTests
{
    private static Action Add(string environment, string? issuer) => () =>
        new ServiceCollection().AddAuthTokenValidation(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:Issuer"] = issuer }).Build(),
            new HostingEnvironment { EnvironmentName = environment });

    [Fact]
    public void AddAuthTokenValidation_InProductionWithAnHttpIssuer_Throws() =>
        Add(Environments.Production, "http://auth.example/").Should().Throw<InvalidOperationException>().WithMessage("*https*");

    [Fact]
    public void AddAuthTokenValidation_InProductionWithoutAnIssuer_Throws() =>
        Add(Environments.Production, null).Should().Throw<InvalidOperationException>().WithMessage("*Auth:Issuer*");

    [Fact]
    public void AddAuthTokenValidation_InProductionWithAnHttpsIssuer_Works() =>
        Add(Environments.Production, "https://indypos-auth.exconeer.com/").Should().NotThrow();
}
