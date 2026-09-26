using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.Entities.Core;

/// <summary>Cash taken out of the drawer (รายจ่าย), tagged Hardware or General.</summary>
public class CashPayout : CashDrawerEntry
{
    public PayoutCategory Category { get; set; } = PayoutCategory.General;
    public string? Description { get; set; }
}
