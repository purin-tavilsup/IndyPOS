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
    /// The store-local calendar date an instant falls on — the same boundary (plain midnight in the
    /// store's timezone) the cash-drawer clock uses. Unspecified kind is read as UTC.
    /// </summary>
    public static DateOnly BusinessDateOf(DateTime utc, TimeZoneInfo storeTimeZone)
    {
        var asUtc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(asUtc, storeTimeZone));
    }
}
