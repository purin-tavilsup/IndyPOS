using IndyPOS.Application.Abstractions.Cloud.Auth;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure.Auth;

public enum StoreDeactivationOutcome { Deactivated, NotFound }

/// <summary>
/// Deactivates a store: the safety valve for a stolen till PC. Auth's client is disabled first, because that is what
/// stops new tokens; only then is the store marked inactive here, so a failure leaves it looking as active as it is.
/// Final: a store that comes back is registered again as a new store. A token issued before keeps working until it
/// expires (15 minutes).
/// </summary>
public static class StoreDeactivation
{
    /// <exception cref="AuthUnavailableException">Auth could not disable the client; nothing changed here.</exception>
    public static async Task<StoreDeactivationOutcome> DeactivateAsync(
        CloudDbContext db, IAuthStoreClients authStoreClients, string storeId, CancellationToken cancellationToken)
    {
        var store = await db.StoreConfigs.SingleOrDefaultAsync(s => s.StoreId == storeId, cancellationToken);
        if (store is null)
            return StoreDeactivationOutcome.NotFound;

        await authStoreClients.DisableAsync(storeId, cancellationToken);

        store.IsActive = false;
        store.LastModifiedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return StoreDeactivationOutcome.Deactivated;
    }
}
