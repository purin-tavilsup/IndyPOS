using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// One store's sync counts, for GET /sync/status. The inbox is counted by SourceStoreId, stamped at
/// ingest from the token; events ingested before that column existed are NULL there and are counted
/// for no store.
/// </summary>
internal static class StoreSyncStatusQuery
{
    public static async Task<StoreSyncStatus> CountAsync(CloudDbContext db, string storeId, CancellationToken cancellationToken)
    {
        var inbox = db.SyncedEvents.Where(e => e.SourceStoreId == storeId);

        return new StoreSyncStatus(
            TotalEvents: await inbox.CountAsync(cancellationToken),
            UnprocessedEvents: await inbox.CountAsync(e => e.ProcessedAtUtc == null, cancellationToken),
            ProcessedEvents: await db.ProcessedEvents.CountAsync(e => e.StoreId == storeId, cancellationToken),
            TotalInvoices: await db.Invoices.CountAsync(i => i.StoreId == storeId, cancellationToken));
    }
}

internal sealed record StoreSyncStatus(int TotalEvents, int UnprocessedEvents, int ProcessedEvents, int TotalInvoices);
