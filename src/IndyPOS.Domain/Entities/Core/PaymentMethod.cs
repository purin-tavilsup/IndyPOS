using IndyPOS.Domain.Enums;

namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// A payment method available in the POS. Rows are data (not a hardcoded enum)
/// so government-campaign methods can be added/retired without a redeploy.
/// Code is the stable key stored on payments and used in reports.
/// </summary>
public class PaymentMethod
{
    public string Code { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public PaymentMethodKind Kind { get; set; }
    public bool IsEnabled { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime? ValidFrom { get; set; }   // informational only — not enforced
    public DateTime? ValidTo { get; set; }      // informational only — not enforced
    public string StoreId { get; set; } = default!;
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
}
