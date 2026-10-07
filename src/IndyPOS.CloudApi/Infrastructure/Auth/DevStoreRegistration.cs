using IndyPOS.Application.Abstractions.Cloud.Auth;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;
using Profiles = IndyPOS.StoreProfiles.StoreProfiles;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

/// <summary>
/// Development only: registers each dev store profile as a cloud client with the fixed dev secret, so
/// dev StoreHubs can sync. Production registers stores only through /admin/stores/register, which
/// hands out a random secret.
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
        var credentials = scope.ServiceProvider.GetRequiredService<IStoreClientCredentialStore>();
        var registered = 0;

        foreach (var profile in Profiles.All)
        {
            if (await db.StoreConfigs.AnyAsync(s => s.StoreId == profile.StoreId, cancellationToken))
                continue;

            // The credential first: if it fails, no config row claims a store that cannot sign in, and
            // the next start retries it.
            await credentials.CreateAsync(profile.CloudClientId, Profiles.DevCloudClientSecret, profile.Name,
                                          cancellationToken);
            db.StoreConfigs.Add(new CloudStoreConfig
            {
                StoreId = profile.StoreId,
                StoreName = profile.Name,
                StoreFullName = profile.FullName,
                ClientId = profile.CloudClientId,
                IsActive = true,
                LastModifiedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);
            registered++;
        }

        return registered;
    }
}
