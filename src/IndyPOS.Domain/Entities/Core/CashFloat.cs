namespace IndyPOS.Domain.Entities.Core;

/// <summary>Cash put into the drawer as change (เงินทอน).</summary>
public class CashFloat : CashDrawerEntry
{
    public string? Description { get; set; }
}
