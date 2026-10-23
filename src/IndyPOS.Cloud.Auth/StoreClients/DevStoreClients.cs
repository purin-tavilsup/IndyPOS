using Microsoft.EntityFrameworkCore;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.Cloud.Auth.StoreClients;

/// <summary>
/// Development only: a client for each dev store profile with the fixed dev secret, so the Aspire stack's StoreHubs
/// can sync. Production stores get theirs, with a random secret, when CloudApi registers them.
/// </summary>
internal static class DevStoreClients
{
    /// <returns>How many store clients this call created.</returns>
    public static async Task<int> RegisterAsync(IServiceProvider services, IHostEnvironment environment, CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return 0;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<Data.AuthDbContext>();
        var storeClients = scope.ServiceProvider.GetRequiredService<StoreClientService>();
        var registered = 0;

        foreach (var profile in Profiles.All)
        {
            if (await db.StoreClients.AnyAsync(s => s.StoreId == profile.StoreId, ct))
                continue;

            await storeClients.CreateOrReplaceAsync(profile.StoreId, profile.Name, ct, Profiles.DevCloudClientSecret);
            registered++;
        }

        return registered;
    }
}

/// <summary>Runs <see cref="DevStoreClients"/> when Auth starts; does nothing outside Development.</summary>
internal sealed class DevStoreClientsSeeder(IServiceProvider services, IHostEnvironment environment) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => DevStoreClients.RegisterAsync(services, environment, cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
