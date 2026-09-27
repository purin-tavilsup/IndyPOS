using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>
/// Server-side source of the business date: the store's timezone (Bangkok), plain midnight
/// boundary. The client never supplies the date, so a wrong till clock cannot misfile an entry.
/// </summary>
public sealed class CashDrawerClock(TimeProvider timeProvider, IStoreIdentityService storeIdentity) : ICashDrawerClock
{
    public CashDrawerInstant Now()
    {
        var utc = timeProvider.GetUtcNow().UtcDateTime;
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, storeIdentity.TimeZone);
        return new CashDrawerInstant(utc, DateOnly.FromDateTime(local));
    }
}
