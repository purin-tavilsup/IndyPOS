using FluentAssertions;
using IndyPOS.Application.Abstractions.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IndyPOS.Application.Tests;

// A dev store must never read the installed store's secrets in %ProgramData%\IndyPOS\Secrets.
public class ConfigureServicesSecretsTests
{
    // A key no real install uses: if the override ever breaks, this test litters ProgramData with a
    // throwaway file instead of overwriting a real secret.
    private static readonly string SecretKey = $"IndyPOS.Tests:SecretsDirectory:{Guid.NewGuid():N}";

    [Fact]
    public async Task AddStoreHubServices_WithASecretsDirectory_StoresSecretsThere()
    {
        var directory = Directory.CreateTempSubdirectory("indypos-secrets-").FullName;
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Secrets:Directory"] = directory })
            .Build();
        var services = new ServiceCollection().AddLogging()
                                              .AddStoreHubServices(configuration);
        await using var provider = services.BuildServiceProvider();

        await provider.GetRequiredService<ISecretStorage>().SetSecretAsync(SecretKey, "value");

        Directory.EnumerateFiles(directory).Should()
                                           .NotBeEmpty();
    }
}
