using System.Net;
using FluentAssertions;
using IndyPOS.Cloud.Auth.StoreClients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.Cloud.Auth.IntegrationTests;

/// <summary>
/// The Aspire stack's dev stores sign in to the dev cloud with a fixed secret. Auth creates their clients in
/// Development only; production stores get theirs from register-store.
/// </summary>
public class DevStoreClientsTests(AuthPostgresFixture postgres) : IClassFixture<AuthPostgresFixture>
{
    private sealed class HostIn(string connectionString, string environment) : AuthTestHost(connectionString,
        new Dictionary<string, string?> { ["OpenIddict:TlsTerminatedUpstream"] = "true" })
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment(environment);
        }
    }

    private static async Task<List<string>> StoreIdsAsync(string connectionString)
    {
        await using var db = AuthPostgresFixture.CreateContext(connectionString);
        return await db.StoreClients.Select(s => s.StoreId).OrderBy(s => s).ToListAsync();
    }

    [Fact]
    public async Task DevStoreClients_InDevelopment_RegistersEveryProfile()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var host = new HostIn(connectionString, Environments.Development);

        host.CreateClient();

        (await StoreIdsAsync(connectionString)).Should().Equal(Profiles.All.Select(p => p.StoreId).OrderBy(s => s));
    }

    [Fact]
    public async Task DevStoreClients_RunTwice_RegistersNothingNew()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using (var first = new HostIn(connectionString, Environments.Development))
            first.CreateClient();
        await using var second = new HostIn(connectionString, Environments.Development);

        var registered = await DevStoreClients.RegisterAsync(second.Services, second.Services.GetRequiredService<IHostEnvironment>(), CancellationToken.None);

        registered.Should().Be(0);
    }

    [Fact]
    public async Task DevStoreClients_OutsideDevelopment_RegistersNothing()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var host = new HostIn(connectionString, "Testing");

        host.CreateClient();

        (await StoreIdsAsync(connectionString)).Should().BeEmpty();
    }

    [Fact]
    public async Task DevStoreClients_TokenWithTheDevSecret_Works()
    {
        var connectionString = await postgres.CreateDatabaseAsync();
        await using var host = new HostIn(connectionString, Environments.Development);
        var profile = Profiles.All[0];

        var response = await TokenHosts.RequestTokenAsync(host, profile.CloudClientId, Profiles.DevCloudClientSecret);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
