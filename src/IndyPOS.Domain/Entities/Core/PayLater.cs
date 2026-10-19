namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core PayLater entity for StoreHub.
/// Tracks deferred payments with partial payment support.
/// </summary>
public class PayLater
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = default!;  // Customer name/identifier
    public decimal PayLaterAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// The v3 PayLater.PaymentId this debt was migrated from, or null for a debt v4 created. Tells the cloud a
    /// debt came from v3 (spec 2026-10-19 §3), the way Payment.LegacyPaymentId does for its row.
    /// </summary>
    public int? LegacyPaymentId { get; set; }

    // Navigation properties
    public Payment Payment { get; set; } = default!;
    public Invoice Invoice { get; set; } = default!;

    // Calculated property
    public decimal RemainingAmount => PayLaterAmount - PaidAmount;
}
