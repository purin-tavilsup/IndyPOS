using System.Text.Json;
using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>
/// Builds the sync event for a cash-drawer change. The payload is the FULL current row — a state
/// snapshot the cloud upserts by Id, never a delta.
/// </summary>
public static class CashDrawerOutbox
{
    public const string CashPayoutChanged = "CashPayoutChanged";
    public const string CashFloatChanged = "CashFloatChanged";
    public const string DebtRepaymentChanged = "DebtRepaymentChanged";
    public const string CashCountChanged = "CashCountChanged";

    public static OutboxEvent Changed(CashDrawerEntry entry, DateTime utcNow) =>
        Create(EventTypeFor(entry), entry.StoreId, entry, utcNow);

    public static OutboxEvent CountAdded(CashCount count, DateTime utcNow) =>
        Create(CashCountChanged, count.StoreId, count, utcNow);

    private static string EventTypeFor(CashDrawerEntry entry) => entry switch
    {
        CashPayout => CashPayoutChanged,
        CashFloat => CashFloatChanged,
        DebtRepayment => DebtRepaymentChanged,
        _ => throw new ArgumentOutOfRangeException(nameof(entry), entry.GetType().Name, "No sync event for this entry type.")
    };

    private static OutboxEvent Create(string type, string storeId, object row, DateTime utcNow) => new()
    {
        Id = Guid.NewGuid(),
        StoreId = storeId,
        Type = type,
        PayloadJson = JsonSerializer.Serialize(row, row.GetType()),
        CreatedUtc = utcNow,
        Status = "Pending"
    };
}
