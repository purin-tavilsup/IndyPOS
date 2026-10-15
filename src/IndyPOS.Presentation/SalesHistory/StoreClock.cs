using IndyPOS.Application.Common.Authorization;

namespace IndyPOS.Presentation.SalesHistory;

/// <summary>
/// The store's own calendar: "today" is plain midnight in the store's time zone, the boundary StoreHub's
/// today-only rule uses, and times are shown store-local. StoreHub sends UTC.
/// </summary>
public sealed class StoreClock(TimeProvider time, TimeZoneInfo storeTimeZone)
{
    public DateOnly Today => BusinessDateOf(time.GetUtcNow().UtcDateTime);

    // JSON can leave the Kind Unspecified; StoreHub always means UTC.
    public DateTime ToStoreTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), storeTimeZone);

    public DateOnly BusinessDateOf(DateTime utc) =>
        TodayOnlyRule.BusinessDateOf(DateTime.SpecifyKind(utc, DateTimeKind.Utc), storeTimeZone);
}
