using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.Common.Authorization;

/// <summary>
/// "Today's figures yes, other days no" (spec §5). A caller without reports.view is held to the
/// store's current business date. Shared by /sales and /cash so the two cannot drift.
/// </summary>
public static class TodayOnlyRule
{
    public static bool Allows(DateOnly day, DateOnly today, bool canViewAnyDay) =>
        canViewAnyDay || day == today;

    public static void EnsureAllowed(DateOnly day, DateOnly today, bool canViewAnyDay)
    {
        if (!Allows(day, today, canViewAnyDay))
            throw new OtherDayForbiddenException();
    }

    /// <summary>
    /// The store-local calendar date an instant falls on: plain midnight in the store's timezone,
    /// the boundary the cash-drawer clock uses. An Unspecified value is read as UTC (ConvertTimeFromUtc's
    /// own rule). A Local value throws ArgumentException rather than being silently misread.
    /// </summary>
    public static DateOnly BusinessDateOf(DateTime utc, TimeZoneInfo storeTimeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, storeTimeZone));
}
