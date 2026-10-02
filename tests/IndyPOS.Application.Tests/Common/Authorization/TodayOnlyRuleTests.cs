using FluentAssertions;
using IndyPOS.Application.Common.Authorization;
using IndyPOS.Application.Common.Exceptions;
using Xunit;

namespace IndyPOS.Application.Tests.Common.Authorization;

public class TodayOnlyRuleTests
{
    private static readonly TimeZoneInfo Bangkok = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
    private static readonly DateOnly Today = new(2026, 9, 27);

    /// <summary>00:05 Bangkok on 27 Sep = 17:05 UTC on 26 Sep.</summary>
    private static readonly DateTime JustAfterBangkokMidnightUtc = new(2026, 9, 26, 17, 5, 0, DateTimeKind.Utc);

    /// <summary>23:55 Bangkok on 26 Sep = 16:55 UTC on 26 Sep.</summary>
    private static readonly DateTime JustBeforeBangkokMidnightUtc = new(2026, 9, 26, 16, 55, 0, DateTimeKind.Utc);

    [Fact]
    public void Allows_WithTodayOnlyAndYesterday_ReturnsFalse()
    {
        TodayOnlyRule.Allows(Today.AddDays(-1), Today, canViewAnyDay: false).Should()
                                                                             .BeFalse();
    }

    [Fact]
    public void Allows_WithTodayOnlyAndTomorrow_ReturnsFalse()
    {
        TodayOnlyRule.Allows(Today.AddDays(1), Today, canViewAnyDay: false).Should()
                                                                            .BeFalse();
    }

    [Fact]
    public void EnsureAllowed_WithTodayOnlyAndYesterday_Throws()
    {
        var act = () => TodayOnlyRule.EnsureAllowed(Today.AddDays(-1), Today, canViewAnyDay: false);

        act.Should()
           .Throw<OtherDayForbiddenException>();
    }

    [Fact]
    public void BusinessDateOf_WithUtcEveningAfterBangkokMidnight_ReturnsTheNextDay()
    {
        TodayOnlyRule.BusinessDateOf(JustAfterBangkokMidnightUtc, Bangkok).Should()
                                                                          .Be(Today);
    }

    [Fact]
    public void BusinessDateOf_WithUtcEveningBeforeBangkokMidnight_ReturnsThatDay()
    {
        TodayOnlyRule.BusinessDateOf(JustBeforeBangkokMidnightUtc, Bangkok).Should()
                                                                           .Be(Today.AddDays(-1));
    }

    [Fact]
    public void BusinessDateOf_WithUnspecifiedKind_TreatsItAsUtc()
    {
        // InMemory and some Npgsql paths hand back Unspecified; ConvertTimeFromUtc would throw on Local.
        var unspecified = DateTime.SpecifyKind(JustAfterBangkokMidnightUtc, DateTimeKind.Unspecified);

        TodayOnlyRule.BusinessDateOf(unspecified, Bangkok).Should()
                                                          .Be(Today);
    }

    [Fact]
    public void Allows_WithTodayOnlyAndToday_ReturnsTrue()
    {
        TodayOnlyRule.Allows(Today, Today, canViewAnyDay: false).Should()
                                                                .BeTrue();
    }

    [Fact]
    public void Allows_WithAnyDayAndYesterday_ReturnsTrue()
    {
        TodayOnlyRule.Allows(Today.AddDays(-1), Today, canViewAnyDay: true).Should()
                                                                            .BeTrue();
    }
}
