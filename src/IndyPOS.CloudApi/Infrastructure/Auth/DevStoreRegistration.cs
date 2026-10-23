using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Development only: records each dev store profile, so dev StoreHubs can sync. Their clients, with the fixed dev
/// secret, are Cloud.Auth's (DevStoreClients). Production registers stores only through /admin/stores/register.
/// </summary>
public static class DevStoreRegistration
{
    /// <returns>How many stores were registered by this call.</returns>
    public static async Task<int> RegisterAsync(IServiceProvider services, IHostEnvironment environment,
                                                CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            return 0;

        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        var registered = 0;

        foreach (var profile in Profiles.All)
        {
            if (await db.StoreConfigs.AnyAsync(s => s.StoreId == profile.StoreId, cancellationToken))
                continue;

            db.StoreConfigs.Add(new CloudStoreConfig
            {
                StoreId = profile.StoreId,
                StoreName = profile.Name,
                StoreFullName = profile.FullName,
                ClientId = profile.CloudClientId,
                IsActive = true,
                LastModifiedAtUtc = DateTime.UtcNow
            });
            registered++;
        }

        await db.SaveChangesAsync(cancellationToken);
        return registered;
    }
}
