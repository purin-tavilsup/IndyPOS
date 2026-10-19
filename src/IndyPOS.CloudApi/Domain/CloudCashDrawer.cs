namespace IndyPOS.CloudApi.Domain;

/// <summary>What the cash mirror's upsert needs from every row: its key, its store and its version.</summary>
public interface ICloudCashRow
{
    Guid Id { get; }
    string StoreId { get; }
    DateTime LastModifiedAtUtc { get; }
}

/// <summary>
/// A cash payout (รายจ่าย), mirrored from StoreHub's cash_payout row by CashPayoutChanged. Mutable:
/// each event carries the full row and the newest LastModifiedAtUtc wins.
/// </summary>
public class CloudCashPayout : ICloudCashRow
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}

/// <summary>A cash float (เงินทอน), mirrored by CashFloatChanged. Same rules as <see cref="CloudCashPayout"/>.</summary>
public class CloudCashFloat : ICloudCashRow
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}

/// <summary>A debt repayment (ลูกค้าชำระหนี้), mirrored by DebtRepaymentChanged. Same rules as <see cref="CloudCashPayout"/>.</summary>
public class CloudDebtRepayment : ICloudCashRow
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}

/// <summary>
/// One cash count of the drawer, mirrored by CashCountChanged. Insert-only, like the store's row:
/// a recount is a new row. The total is not stored; it is the sum of the counts times their value.
/// </summary>
public class CloudCashCount : ICloudCashRow
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public DateOnly BusinessDate { get; set; }
    public int BankNote1000Count { get; set; }
    public int BankNote500Count { get; set; }
    public int BankNote100Count { get; set; }
    public int BankNote50Count { get; set; }
    public int BankNote20Count { get; set; }
    public int Coin10Count { get; set; }
    public int Coin5Count { get; set; }
    public int Coin2Count { get; set; }
    public int Coin1Count { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}
