using System.Globalization;
using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Infrastructure.QueryHandlers.Reports;
using IndyPOS.Mock;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashDrawerClockTests
{
    /// <summary>23:59:59 Bangkok on 2026-09-26 is 16:59:59 UTC the same day.</summary>
    private static readonly DateTimeOffset OneSecondBeforeBangkokMidnight = new(2026, 9, 26, 16, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset BangkokMidnight = new(2026, 9, 26, 17, 0, 0, TimeSpan.Zero);

    // The cloud files a sale on the day this clock gives it; the till's summary finds a day's sales by
    // ReportDateRange. The two agree only if every instant lies inside its own day's range (review finding on
    // cloud-gaps PR 3), including the early-morning hours that are still yesterday in UTC.
    [Theory]
    [InlineData("2026-09-26T16:59:59Z")]   // 23:59:59 Bangkok
    [InlineData("2026-09-26T17:00:00Z")]   // 00:00 Bangkok, the next day
    [InlineData("2026-09-26T17:30:00Z")]   // 00:30 Bangkok
    [InlineData("2026-09-25T23:40:00Z")]   // 06:40 Bangkok, an early v3 opening time
    [InlineData("2026-09-26T05:00:00Z")]   // noon Bangkok
    public void Now_WithAnyInstant_FallsInsideItsDaysReportRange(string utc)
    {
        var instant = ClockAt(DateTimeOffset.Parse(utc, CultureInfo.InvariantCulture)).Now();

        var range = ReportDateRange.ToUtcRange(instant.BusinessDate, instant.BusinessDate, CashDrawerTestContext.Bangkok);

        instant.Utc.Should()
                   .BeOnOrAfter(range.StartUtc)
                   .And.BeBefore(range.EndExclusiveUtc);
    }

    private static CashDrawerClock ClockAt(DateTimeOffset utcNow)
    {
        var store = MockStoreIdentityService.GeneralHardware();
        store.TimeZone = CashDrawerTestContext.Bangkok;
        return new CashDrawerClock(new FakeTimeProvider(utcNow), store);
    }

    [Fact]
    public void Now_WhenOneSecondBeforeBangkokMidnight_ReturnsTheSameBusinessDate()
    {
        var clock = ClockAt(OneSecondBeforeBangkokMidnight);

        var now = clock.Now();

        now.BusinessDate.Should()
                        .Be(new DateOnly(2026, 9, 26));
    }

    [Fact]
    public void Now_WhenAtBangkokMidnight_ReturnsTheNextBusinessDate()
    {
        var clock = ClockAt(BangkokMidnight);

        var now = clock.Now();

        now.BusinessDate.Should()
                        .Be(new DateOnly(2026, 9, 27));
    }

    [Fact]
    public void Now_WhenUtcDateIsStillYesterday_ReturnsTheBangkokDate()
    {
        var clock = ClockAt(new DateTimeOffset(2026, 9, 25, 23, 30, 0, TimeSpan.Zero)); // 06:30 Bangkok on 26th

        var now = clock.Now();

        now.BusinessDate.Should()
                        .Be(new DateOnly(2026, 9, 26));
    }

    [Fact]
    public void Now_WithAnyTime_ReturnsUtcKind()
    {
        var clock = ClockAt(BangkokMidnight);

        var now = clock.Now();

        now.Utc.Kind.Should()
                    .Be(DateTimeKind.Utc);
    }
}
