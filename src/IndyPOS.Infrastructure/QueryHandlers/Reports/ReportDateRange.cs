namespace IndyPOS.Infrastructure.QueryHandlers.Reports;

public static class ReportDateRange
{
    public static UtcDateTimeRange ToUtcRange(
        DateOnly fromDate,
        DateOnly toDate,
        TimeZoneInfo? timeZone = null)
    {
        if (toDate < fromDate)
        {
            throw new ArgumentException("Report end date must be on or after start date.", nameof(toDate));
        }

        var reportTimeZone = timeZone ?? TimeZoneInfo.Local;
        var startLocal = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var endExclusiveLocal = toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        return new UtcDateTimeRange(
            TimeZoneInfo.ConvertTimeToUtc(startLocal, reportTimeZone),
            TimeZoneInfo.ConvertTimeToUtc(endExclusiveLocal, reportTimeZone));
    }
}

public readonly record struct UtcDateTimeRange(DateTime StartUtc, DateTime EndExclusiveUtc);
