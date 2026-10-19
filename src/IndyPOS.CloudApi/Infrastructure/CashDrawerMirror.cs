using System.Text.Json;
using IndyPOS.Application.Abstractions.Cloud.Repositories;
using IndyPOS.CloudApi.Domain;
using IndyPOS.Domain.Entities.Core;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.CloudApi.Infrastructure;

/// <summary>
/// Turns a cash-drawer event into the cloud mirror row. Unlike InvoiceCompleted these rows change
/// (edit, delete), and each event carries the whole row, so it is an upsert by Id. The newest
/// LastModifiedUtc wins: a retry or an out-of-order delivery can never roll a newer edit back.
/// Payloads are deserialized into the domain types StoreHub serialized, so the contract cannot drift.
/// </summary>
internal static class CashDrawerMirror
{
    public static Task ProcessPayoutAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct)
    {
        var payout = Read<CashPayout>(syncedEvent);
        return UpsertAsync(syncedEvent, db.CashPayouts, new CloudCashPayout
        {
            Id = payout.Id, StoreId = payout.StoreId, Category = payout.Category.ToString(), Amount = payout.Amount,
            Description = payout.Description, BusinessDate = payout.BusinessDate,
            CreatedAtUtc = Utc(payout.CreatedUtc), LastModifiedAtUtc = Utc(payout.LastModifiedUtc),
            CreatedByUserId = payout.CreatedByUserId, LastModifiedByUserId = payout.LastModifiedByUserId,
            IsDeleted = payout.IsDeleted, DeletedAtUtc = Utc(payout.DeletedUtc), SyncedAtUtc = DateTime.UtcNow
        }, db, ct);
    }

    public static Task ProcessFloatAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct)
    {
        var cashFloat = Read<CashFloat>(syncedEvent);
        return UpsertAsync(syncedEvent, db.CashFloats, new CloudCashFloat
        {
            Id = cashFloat.Id, StoreId = cashFloat.StoreId, Amount = cashFloat.Amount,
            Description = cashFloat.Description, BusinessDate = cashFloat.BusinessDate,
            CreatedAtUtc = Utc(cashFloat.CreatedUtc), LastModifiedAtUtc = Utc(cashFloat.LastModifiedUtc),
            CreatedByUserId = cashFloat.CreatedByUserId, LastModifiedByUserId = cashFloat.LastModifiedByUserId,
            IsDeleted = cashFloat.IsDeleted, DeletedAtUtc = Utc(cashFloat.DeletedUtc), SyncedAtUtc = DateTime.UtcNow
        }, db, ct);
    }

    public static Task ProcessDebtRepaymentAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct)
    {
        var repayment = Read<DebtRepayment>(syncedEvent);
        return UpsertAsync(syncedEvent, db.DebtRepayments, new CloudDebtRepayment
        {
            Id = repayment.Id, StoreId = repayment.StoreId, CustomerName = repayment.CustomerName,
            Amount = repayment.Amount, BusinessDate = repayment.BusinessDate,
            CreatedAtUtc = Utc(repayment.CreatedUtc), LastModifiedAtUtc = Utc(repayment.LastModifiedUtc),
            CreatedByUserId = repayment.CreatedByUserId, LastModifiedByUserId = repayment.LastModifiedByUserId,
            IsDeleted = repayment.IsDeleted, DeletedAtUtc = Utc(repayment.DeletedUtc), SyncedAtUtc = DateTime.UtcNow
        }, db, ct);
    }

    // Insert-only falls out of the stale rule: a count is never edited, so a stored Id is never older.
    public static Task ProcessCashCountAsync(SyncedEventEntity syncedEvent, CloudDbContext db, CancellationToken ct)
    {
        var count = Read<CashCount>(syncedEvent);
        return UpsertAsync(syncedEvent, db.CashCounts, new CloudCashCount
        {
            Id = count.Id, StoreId = count.StoreId, BusinessDate = count.BusinessDate,
            BankNote1000Count = count.BankNote1000Count, BankNote500Count = count.BankNote500Count,
            BankNote100Count = count.BankNote100Count, BankNote50Count = count.BankNote50Count,
            BankNote20Count = count.BankNote20Count, Coin10Count = count.Coin10Count, Coin5Count = count.Coin5Count,
            Coin2Count = count.Coin2Count, Coin1Count = count.Coin1Count,
            CreatedAtUtc = Utc(count.CreatedUtc), LastModifiedAtUtc = Utc(count.LastModifiedUtc),
            CreatedByUserId = count.CreatedByUserId, SyncedAtUtc = DateTime.UtcNow
        }, db, ct);
    }

    private static async Task UpsertAsync<TCloud>(
        SyncedEventEntity syncedEvent, DbSet<TCloud> set, TCloud incoming, CloudDbContext db, CancellationToken ct)
        where TCloud : class, ICloudCashRow
    {
        var existing = await set.FindAsync([incoming.Id], ct);

        if (existing is null)
            set.Add(incoming);
        else if (existing.StoreId != incoming.StoreId)
            // Ingest checks the payload's StoreId against the token, but not that the row is that
            // store's: throwing keeps another store's row intact and the event visible for review.
            throw new InvalidOperationException(
                $"{syncedEvent.EventType} {syncedEvent.EventId}: row {incoming.Id} belongs to store {existing.StoreId}, not {incoming.StoreId}.");
        else if (incoming.LastModifiedAtUtc > existing.LastModifiedAtUtc)
            db.Entry(existing).CurrentValues.SetValues(incoming);

        // A stale event is still recorded, so it is not retried.
        db.ProcessedEvents.Add(new ProcessedEvent
        {
            EventId = syncedEvent.EventId, EventType = syncedEvent.EventType,
            StoreId = incoming.StoreId, ProcessedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    private static T Read<T>(SyncedEventEntity syncedEvent) =>
        JsonSerializer.Deserialize<T>(syncedEvent.Payload)
        ?? throw new InvalidOperationException($"{syncedEvent.EventType} {syncedEvent.EventId} has an empty payload.");

    // Npgsql refuses an unspecified kind for timestamptz; StoreHub's times are UTC whatever their kind says.
    private static DateTime Utc(DateTime time) => DateTime.SpecifyKind(time, DateTimeKind.Utc);

    private static DateTime? Utc(DateTime? time) => time is { } t ? Utc(t) : null;
}
