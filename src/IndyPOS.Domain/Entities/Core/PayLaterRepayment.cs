namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// One repayment taken against a ลงบัญชี debt on the ลงบัญชี screen: how much, on which store day, and which
/// cashier took it (spec 2026-10-19 §3). Append-only, like a cash count: never edited or deleted. The debt's
/// PaidAmount is the running total; these rows are the audit trail behind it.
/// </summary>
public class PayLaterRepayment
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public Guid PayLaterId { get; set; }
    public decimal Amount { get; set; }

    /// <summary>The store-local day it was taken; set by the server, never by the till.</summary>
    public DateOnly BusinessDate { get; set; }

    public DateTime CreatedUtc { get; set; }

    /// <summary>The cashier who took it, from the login token.</summary>
    public Guid CreatedByUserId { get; set; }
}
