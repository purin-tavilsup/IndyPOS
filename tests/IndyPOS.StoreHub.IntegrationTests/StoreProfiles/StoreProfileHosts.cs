using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.StoreProfiles;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.StoreHub.IntegrationTests.StoreProfiles;

[CollectionDefinition(Name)]
public sealed class StoreProfilesCollection : ICollectionFixture<StoreProfileHosts>
{
    public const string Name = "StoreProfiles";
}

/// <summary>
/// One StoreHub per dev store profile, each on its own database and seeded the way Development seeds
/// it, built once and shared by every per-store test.
/// </summary>
public sealed class StoreProfileHosts : IAsyncLifetime
{
    private readonly Dictionary<string, ProfileFactory> _hosts = new(StringComparer.OrdinalIgnoreCase);

    public async Task InitializeAsync()
    {
        foreach (var profile in Profiles.All)
        {
            var factory = new ProfileFactory(profile);
            await factory.InitializeAsync();
            await SeedLikeDevelopmentAsync(factory.Services);
            _hosts[profile.Key] = factory;
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var factory in _hosts.Values)
            await factory.DisposeAsync();
    }

    public IServiceProvider ServicesFor(string key) => _hosts[key].Services;

    public async Task<HttpClient> SignedInAsync(string key, string username = "cashier", string password = "cashier123")
    {
        var client = _hosts[key].CreateClient();
        var login = await client.PostAsJsonAsync("/auth/login", new { username, password });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<LoginResponse>())!.Token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    // Mirrors Program.cs's Development block (catalogues first, then the dev data, which switches the
    // store's payment methods), minus the migrate: the tests' schema comes from EnsureCreated on a fresh
    // database, like the rest of this suite.
    internal static async Task SeedLikeDevelopmentAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<StoreHubDbContext>().Database.EnsureCreatedAsync();
        await scope.ServiceProvider.GetRequiredService<PaymentMethodSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<ProductCategorySeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>().SeedAsync();
    }

    private sealed class ProfileFactory(StoreProfile profile) : StoreHubWebApplicationFactory
    {
        protected override IStoreIdentityService CreateStoreIdentity() =>
            new TestStoreIdentityService(profile.StoreId, profile.Type);
    }
}
