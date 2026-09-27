namespace IndyPOS.Infrastructure.Persistence.StoreHub;

/// <summary>
/// Names of the global query filters on the cash-drawer tables. Named (EF 10) so a later filter
/// can be added or bypassed without disturbing this one.
/// </summary>
public static class CashDrawerQueryFilters
{
    /// <summary>
    /// Hides soft-deleted rows from every query. This is the repo's first global filter, on purpose:
    /// a forgotten Where would put a deleted payout back into the drawer's money.
    /// </summary>
    public const string SoftDelete = "SoftDelete";
}
