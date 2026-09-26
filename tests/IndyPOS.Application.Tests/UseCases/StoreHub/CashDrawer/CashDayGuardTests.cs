using FluentAssertions;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.CashDrawer;

public class CashDayGuardTests
{
    private static readonly DateOnly Today = new(2026, 9, 26);

    [Fact]
    public void EnsureEditable_WithYesterday_Throws()
    {
        var act = () => CashDayGuard.EnsureEditable(Today.AddDays(-1), Today);

        act.Should()
           .Throw<CashDayClosedException>();
    }

    [Fact]
    public void EnsureEditable_WithFutureDate_Throws()
    {
        var act = () => CashDayGuard.EnsureEditable(Today.AddDays(1), Today);

        act.Should()
           .Throw<CashDayClosedException>();
    }

    [Fact]
    public void EnsureEditable_WithToday_DoesNotThrow()
    {
        var act = () => CashDayGuard.EnsureEditable(Today, Today);

        act.Should()
           .NotThrow();
    }
}
