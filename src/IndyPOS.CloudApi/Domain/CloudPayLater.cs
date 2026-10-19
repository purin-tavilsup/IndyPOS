namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// A ลงบัญชี (PayLater) debt, mirrored from StoreHub's pay_later row by PayLaterChanged. Each event carries
/// the whole debt, and the newest or equal LastModifiedAtUtc wins.
/// </summary>
public class CloudPayLater
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Guid PaymentId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal PayLaterAmount { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Computed by the database: PayLaterAmount − PaidAmount.</summary>
    public decimal OwedAmount { get; private set; }

    public bool IsCompleted { get; set; }
    public int? LegacyPaymentId { get; set; }

    /// <summary>Computed by the database: LegacyPaymentId is not null.</summary>
    public bool IsFromV3 { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}

/// <summary>One repayment taken on a store's ลงบัญชี screen, mirrored by PayLaterRepaid. Insert-only.</summary>
public class CloudPayLaterRepayment
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public Guid PayLaterId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}
