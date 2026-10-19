using System.Text.Json;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.CloudApi.Domain;
using Microsoft.EntityFrameworkCore;

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

    /// <summary>
    /// Insert-only: a repayment is never edited, so a stored Id is kept. A repayment can reach the inbox before
    /// its debt's snapshot (both are saved together at the store); throwing leaves it unprocessed, so the retry
    /// backoff tries again once the debt has landed. Matching the debt's store as well keeps a payload that names
    /// another store's debt out of both stores' records.
    /// </summary>
    public static async Task ProcessRepaidAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct)
    {
        var repayment = Read<PayLaterRepaidPayload>(syncedEvent);

        // StoreHub refuses these too, but the cloud is its own boundary: an insert-only audit row cannot be undone.
        if (repayment.Amount <= 0)
            throw new InvalidOperationException(
                $"Repayment {repayment.Id} of debt {repayment.PayLaterId} has amount {repayment.Amount}; a repayment must be above 0. It is kept for review, not stored.");

        var debtArrived = await db.PayLaters.AnyAsync(d => d.Id == repayment.PayLaterId && d.StoreId == repayment.StoreId, ct);

        if (!debtArrived)
            throw new InvalidOperationException(
                $"Repayment {repayment.Id} has no debt {repayment.PayLaterId} in store {repayment.StoreId} yet; it is retried. If this keeps failing, the debt may belong to another store.");

        if (await db.PayLaterRepayments.FindAsync([repayment.Id], ct) is null)
            db.PayLaterRepayments.Add(new CloudPayLaterRepayment
            {
                Id = repayment.Id, StoreId = repayment.StoreId, PayLaterId = repayment.PayLaterId, Amount = repayment.Amount,
                BusinessDate = repayment.BusinessDate, CreatedAtUtc = Utc(repayment.CreatedUtc),
                CreatedByUserId = repayment.CreatedByUserId, SyncedAtUtc = DateTime.UtcNow
            });

        MarkProcessed(syncedEvent, repayment.StoreId, db);
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
