using FluentAssertions;
using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.CloudApi.Infrastructure;
using IndyPOS.CloudApi.Infrastructure.Auth;
using IndyPOS.Testing.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using Moq;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.CloudApi.IntegrationTests;

/// <summary>
/// Dev stores sync only because CloudApi's Development startup registers them with a fixed dev secret.
/// Outside Development it must register nothing: production stores come only from the admin route.
/// </summary>
public class DevStoreRegistrationTests : IAsyncLifetime
{
    private TestPostgres _postgres = null!;
    private readonly Mock<IStoreClientCredentialStore> _credentials = new();

    public async Task InitializeAsync() => _postgres = await TestPostgres.StartAsync();

    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task DevStoreRegistration_OutsideDevelopment_RegistersNothing()
    {
        await using var services = await BuildServicesAsync();

        var registered = await DevStoreRegistration.RegisterAsync(services, EnvironmentNamed(Environments.Production),
                                                                  CancellationToken.None);

        registered.Should()
                  .Be(0);
    }

    [Fact]
    public async Task DevStoreRegistration_RunTwice_RegistersEachStoreOnce()
    {
        await using var services = await BuildServicesAsync();
        var development = EnvironmentNamed(Environments.Development);
        await DevStoreRegistration.RegisterAsync(services, development, CancellationToken.None);

        var second = await DevStoreRegistration.RegisterAsync(services, development, CancellationToken.None);

        second.Should()
              .Be(0);
        (await StoreIdsAsync(services)).Should()
                                       .BeEquivalentTo(Profiles.All.Select(p => p.StoreId));
    }

    [Fact]
    public async Task DevStoreRegistration_InDevelopment_GivesEachStoreTheDevSecret()
    {
        await using var services = await BuildServicesAsync();

        await DevStoreRegistration.RegisterAsync(services, EnvironmentNamed(Environments.Development),
                                                 CancellationToken.None);

        foreach (var profile in Profiles.All)
        {
            _credentials.Verify(c => c.CreateAsync(profile.CloudClientId, Profiles.DevCloudClientSecret,
                                                   It.IsAny<string>(), It.IsAny<CancellationToken>()),
                                Times.Once);
        }
    }

    private async Task<ServiceProvider> BuildServicesAsync()
    {
        var services = new ServiceCollection()
            .AddDbContext<CloudDbContext>(options => options.UseNpgsql(_postgres.ConnectionString))
            .AddSingleton(_credentials.Object)
            .BuildServiceProvider();

        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CloudDbContext>().Database.MigrateAsync();
        return services;
    }

    private static async Task<List<string>> StoreIdsAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<CloudDbContext>().StoreConfigs
                          .Select(s => s.StoreId).ToListAsync();
    }

    private static IHostEnvironment EnvironmentNamed(string name) => new HostingEnvironment { EnvironmentName = name };
}
