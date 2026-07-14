using FluentAssertions;
using IndyPOS.Vault;
using Xunit;

namespace IndyPOS.Vault.Tests;

public class VaultArchitectureTests
{
    [Fact]
    public void Vault_ShouldNotReferenceApplicationInfrastructureOrDomain()
    {
        var referenced = typeof(SecretProtector).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name);

        referenced.Should().NotContain(name =>
            name == "IndyPOS.Application" ||
            name == "IndyPOS.Infrastructure" ||
            name == "IndyPOS.Domain" ||
            name == "IndyPOS.ServiceDefaults");
    }
}
