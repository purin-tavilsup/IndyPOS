using System.Security.Cryptography;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Cloud.Auth.Data;
using IndyPOS.Cloud.Auth.Domain;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace IndyPOS.Cloud.Auth.StoreClients;

internal sealed record StoreClientCredentials(string ClientId, string ClientSecret);

/// <summary>
/// A store's OpenIddict client and Auth's record of it, kept together. A disabled store stays disabled: creating it
/// again is refused, because a store that comes back is registered as a new store.
/// </summary>
internal sealed class StoreClientService(AuthDbContext db, IOpenIddictApplicationManager applications)
{
    public static string ClientIdFor(string storeId) => $"store_{storeId}";

    /// <param name="secret">A fixed secret (the dev stores' only); null makes a random one.</param>
    /// <returns>The credentials, or null when the store was disabled.</returns>
    /// <remarks>
    /// Serialised per store: the row is inserted if missing and locked (FOR UPDATE) before the OpenIddict client is
    /// written, all in one transaction, so a retried or doubled call waits and then replaces the secret instead of
    /// interleaving. OpenIddict's stores use this same DbContext, so they write inside the transaction.
    /// </remarks>
    public async Task<StoreClientCredentials?> CreateOrReplaceAsync(string storeId, string displayName, CancellationToken ct, string? secret = null)
    {
        var credentials = new StoreClientCredentials(ClientIdFor(storeId), secret ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
        var strategy = db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync(ct);

            var record = await LockAsync(storeId, credentials.ClientId, ct);
            if (!record.Enabled)
                return null;

            await WriteApplicationAsync(credentials, displayName, ct);
            await transaction.CommitAsync(ct);
            return credentials;
        });
    }

    private async Task<StoreClient> LockAsync(string storeId, string clientId, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""INSERT INTO "StoreClients" ("StoreId", "ClientId", "Enabled", "CreatedAtUtc") VALUES ({storeId}, {clientId}, TRUE, {DateTime.UtcNow}) ON CONFLICT ("StoreId") DO NOTHING""",
            ct);

        return await db.StoreClients.FromSqlInterpolated($"""SELECT * FROM "StoreClients" WHERE "StoreId" = {storeId} FOR UPDATE""")
                                    .AsTracking()
                                    .SingleAsync(ct);
    }

    private async Task WriteApplicationAsync(StoreClientCredentials credentials, string displayName, CancellationToken ct)
    {
        var descriptor = Descriptor(credentials, displayName);

        if (await applications.FindByClientIdAsync(credentials.ClientId, ct) is { } application)
            await applications.UpdateAsync(application, descriptor, ct);
        else
            await applications.CreateAsync(descriptor, ct);
    }

    public async Task DisableAsync(string storeId, CancellationToken ct)
    {
        var record = await db.StoreClients.FindAsync([storeId], ct);
        if (record is null || !record.Enabled)
            return;

        record.Enabled = false;
        record.DisabledAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(string storeId, CancellationToken ct)
    {
        if (await applications.FindByClientIdAsync(ClientIdFor(storeId), ct) is { } application)
            await applications.DeleteAsync(application, ct);

        await db.StoreClients.Where(s => s.StoreId == storeId).ExecuteDeleteAsync(ct);
    }

    private static OpenIddictApplicationDescriptor Descriptor(StoreClientCredentials credentials, string displayName) => new()
    {
        ClientId = credentials.ClientId,
        ClientSecret = credentials.ClientSecret,
        ClientType = ClientTypes.Confidential,
        DisplayName = displayName,
        Permissions =
        {
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.ClientCredentials,
            Permissions.Prefixes.Scope + AuthScopes.SyncWrite,
            Permissions.Prefixes.Scope + AuthScopes.MasterRead
        }
    };
}
