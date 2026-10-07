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

            await RegisterAtomicallyAsync(db, credentials, profile, cancellationToken);
            registered++;
        }

        return registered;
    }

    // The row and its client in one transaction (as RegisterStoreHandler does): a start that dies in
    // between must not leave a client behind, because OpenIddict refuses the duplicate on every later
    // start. The retrying execution strategy only allows a transaction inside ExecuteAsync, which may
    // replay the lambda, so each attempt starts from a clean change tracker.
    private static async Task RegisterAtomicallyAsync(CloudDbContext db, IStoreClientCredentialStore credentials,
                                                      IndyPOS.StoreProfiles.StoreProfile profile,
                                                      CancellationToken cancellationToken)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

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
            await credentials.CreateAsync(profile.CloudClientId, Profiles.DevCloudClientSecret, profile.Name,
                                          cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }
}
