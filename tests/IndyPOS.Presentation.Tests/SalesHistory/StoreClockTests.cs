using FluentAssertions;
using IndyPOS.Presentation.SalesHistory;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace IndyPOS.Presentation.Tests.SalesHistory;

public class StoreClockTests
{
    // 23:30 UTC on the 14th is 06:30 on the 15th in Bangkok (UTC+7).
    private static readonly DateTimeOffset LateEveningUtc = new(2026, 10, 14, 23, 30, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    [Fact]
    public void Today_AfterMidnightInBangkokButNotInUtc_IsTheBangkokDate()
    {
        var clock = new StoreClock(new FakeTimeProvider(LateEveningUtc), Bangkok);

        clock.Today.Should()
                   .Be(new DateOnly(2026, 10, 15));
    }

    [Fact]
    public void ToStoreTime_WithAnUnspecifiedKind_ReadsItAsUtc()
    {
        var clock = new StoreClock(new FakeTimeProvider(LateEveningUtc), Bangkok);

        var local = clock.ToStoreTime(new DateTime(2026, 10, 14, 23, 30, 0, DateTimeKind.Unspecified));

        local.Should()
             .Be(new DateTime(2026, 10, 15, 6, 30, 0));
    }

    [Fact]
    public void BusinessDateOf_WithALateEveningUtcSale_IsTheNextDay()
    {
        var clock = new StoreClock(new FakeTimeProvider(LateEveningUtc), Bangkok);

        clock.BusinessDateOf(LateEveningUtc.UtcDateTime).Should()
                                                        .Be(new DateOnly(2026, 10, 15));
    }
}
