using System.Text.Json;
using IndyPOS.Domain.Entities.Core;

using PayLaterDebt = IndyPOS.Domain.Entities.Core.PayLater;

namespace IndyPOS.Application.UseCases.StoreHub.PayLater;

/// <summary>
/// The whole debt as it stands now (spec 2026-10-19 §3). A snapshot, so the cloud upserts it and the newest or
/// equal LastModifiedUtc wins (a re-migrated v3 debt keeps its timestamp but not its invoice); StoreId sits at the top because the cloud's ingest check reads it there.
/// </summary>
public sealed record PayLaterChangedPayload(
    Guid Id,
    string StoreId,
    Guid PaymentId,
    Guid InvoiceId,
    string Description,
    decimal PayLaterAmount,
    decimal PaidAmount,
    bool IsCompleted,
    int? LegacyPaymentId,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc);

/// <summary>One repayment, insert-only in the cloud: the audit trail of who took what, and on which day.</summary>
public sealed record PayLaterRepaidPayload(
    Guid Id,
    string StoreId,
    Guid PayLaterId,
    decimal Amount,
    DateOnly BusinessDate,
    DateTime CreatedUtc,
    Guid CreatedByUserId);

/// <summary>Builds the ลงบัญชี sync events, saved in the same SaveChangesAsync as the change they describe.</summary>
public static class PayLaterOutbox
{
    public const string PayLaterChanged = "PayLaterChanged";
    public const string PayLaterRepaid = "PayLaterRepaid";

    // PayLater has no StoreId column (its store is its invoice's), so the caller supplies it.
    public static OutboxEvent Changed(PayLaterDebt debt, string storeId, DateTime utcNow)
    {
        var payload = new PayLaterChangedPayload(
            debt.Id, storeId, debt.PaymentId, debt.InvoiceId, debt.Description, debt.PayLaterAmount, debt.PaidAmount,
            debt.IsCompleted, debt.LegacyPaymentId, debt.CreatedUtc, debt.LastModifiedUtc);

        return Pending(storeId, PayLaterChanged, JsonSerializer.Serialize(payload), utcNow);
    }

    public static OutboxEvent Repaid(PayLaterRepayment repayment, DateTime utcNow)
    {
        var payload = new PayLaterRepaidPayload(
            repayment.Id, repayment.StoreId, repayment.PayLaterId, repayment.Amount, repayment.BusinessDate,
            repayment.CreatedUtc, repayment.CreatedByUserId);

        return Pending(repayment.StoreId, PayLaterRepaid, JsonSerializer.Serialize(payload), utcNow);
    }

    private static OutboxEvent Pending(string storeId, string type, string payloadJson, DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        Type = type,
        PayloadJson = payloadJson,
        CreatedUtc = utcNow,
        Status = "Pending"
    };
}
