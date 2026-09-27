using FluentAssertions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Mock;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashDrawerClockTests
{
    /// <summary>23:59:59 Bangkok on 2026-09-26 is 16:59:59 UTC the same day.</summary>
    private static readonly DateTimeOffset OneSecondBeforeBangkokMidnight = new(2026, 9, 26, 16, 59, 59, TimeSpan.Zero);
    private static readonly DateTimeOffset BangkokMidnight = new(2026, 9, 26, 17, 0, 0, TimeSpan.Zero);

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
