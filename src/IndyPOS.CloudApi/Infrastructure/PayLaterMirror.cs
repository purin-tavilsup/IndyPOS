using System.Text.Json;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.CloudApi.Domain;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// Turns a ลงบัญชี event into the cloud mirror row. A debt changes with every repayment and each event carries
/// the whole debt, so it is an upsert by Id where the newest OR EQUAL LastModifiedUtc wins: a re-migrated v3
/// debt keeps its id and timestamp but gets a new invoice and payment, and must point at those (spec
/// 2026-10-19 §3). Unlike the cash rows, a tie therefore takes the incoming snapshot. Payloads are read as the
/// records StoreHub's PayLaterOutbox writes, so the contract cannot drift.
/// </summary>
internal static class PayLaterMirror
{
    public static async Task ProcessChangedAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct)
    {
        var debt = Read<PayLaterChangedPayload>(syncedEvent);
        var incoming = ToCloud(debt);
        var existing = await db.PayLaters.FindAsync([incoming.Id], ct);

        if (existing is null)
            db.PayLaters.Add(incoming);
        else if (existing.StoreId != incoming.StoreId)
            // Ingest checks the payload's StoreId against the token, but not that the debt is that store's:
            // throwing keeps another store's debt intact and the event visible for review.
            throw new InvalidOperationException(
                $"{syncedEvent.EventType} {syncedEvent.EventId}: debt {incoming.Id} belongs to store {existing.StoreId}, not {incoming.StoreId}.");
        else if (incoming.LastModifiedAtUtc >= existing.LastModifiedAtUtc)
            db.Entry(existing).CurrentValues.SetValues(incoming);

        // A stale event is still recorded, so it is not retried.
        MarkProcessed(syncedEvent, incoming.StoreId, db);
        await db.SaveChangesAsync(ct);
    }

    private static CloudPayLater ToCloud(PayLaterChangedPayload debt) => new()
    {
        Id = debt.Id, StoreId = debt.StoreId, InvoiceId = debt.InvoiceId, PaymentId = debt.PaymentId,
        CustomerName = debt.Description, PayLaterAmount = debt.PayLaterAmount, PaidAmount = debt.PaidAmount,
        IsCompleted = debt.IsCompleted, LegacyPaymentId = debt.LegacyPaymentId,
        CreatedAtUtc = Utc(debt.CreatedUtc), LastModifiedAtUtc = Utc(debt.LastModifiedUtc), SyncedAtUtc = DateTime.UtcNow
    };

    private static void MarkProcessed(SyncedEventEntity syncedEvent, string storeId, CloudDbContext db) =>
        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = syncedEvent.EventId, EventType = syncedEvent.EventType,
            StoreId = storeId, ProcessedAtUtc = DateTime.UtcNow
        });

    private static T Read<T>(SyncedEventEntity syncedEvent) =>
        JsonSerializer.Deserialize<T>(syncedEvent.Payload)
        ?? throw new InvalidOperationException($"{syncedEvent.EventType} {syncedEvent.EventId} has an empty payload.");

    // Npgsql refuses an unspecified kind for timestamptz; StoreHub's times are UTC whatever their kind says.
    private static DateTime Utc(DateTime time) => DateTime.SpecifyKind(time, DateTimeKind.Utc);
}
