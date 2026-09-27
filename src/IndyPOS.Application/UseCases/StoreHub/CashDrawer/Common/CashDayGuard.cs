using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;

/// <summary>Only today's entries can change; any other day is view-only.</summary>
public static class CashDayGuard
{
    public static void EnsureEditable(DateOnly businessDate, DateOnly today)
    {
        if (businessDate != today)
            throw new CashDayClosedException(businessDate);
    }
}
