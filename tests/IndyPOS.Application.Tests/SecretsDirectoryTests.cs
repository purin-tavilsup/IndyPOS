using FluentAssertions;
using IndyPOS.Infrastructure.Services.Security;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IndyPOS.Application.Tests;

// Only a real path may override where StoreHub keeps its secrets; anything blank means "not set".
public class SecretsDirectoryTests
{
    private static readonly string InstalledDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "IndyPOS", "Secrets");

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Resolve_WithABlankOverride_UsesTheInstalledDirectory(string blank)
    {
        var directory = SecretsDirectory.Resolve(ConfigurationWith(blank));

        directory.Should()
                 .Be(InstalledDirectory);
    }

    [Fact]
    public void Resolve_WithNoOverride_UsesTheInstalledDirectory()
    {
        SecretsDirectory.Resolve(ConfigurationWith(null)).Should()
                                                         .Be(InstalledDirectory);
    }

    [Fact]
    public void Resolve_WithAnOverride_UsesIt()
    {
        const string devDirectory = @"C:\dev\secrets";

        SecretsDirectory.Resolve(ConfigurationWith(devDirectory)).Should()
                                                                 .Be(devDirectory);
    }

    private static IConfiguration ConfigurationWith(string? value) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:Directory"] = value })
                                  .Build();
}
