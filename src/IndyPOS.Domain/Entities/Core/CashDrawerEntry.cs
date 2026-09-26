namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Shared shape of a hand-typed, soft-deletable cash-drawer entry (payout, float, debt repayment).
/// Not an EF entity itself: each subclass maps to its own table.
/// </summary>
public abstract class CashDrawerEntry
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public decimal Amount { get; set; }

    /// <summary>Store-local cash day the entry belongs to. Set once at creation; edits never move it.</summary>
    public DateOnly BusinessDate { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? LastModifiedByUserId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedUtc { get; set; }

    /// <summary>Records who last changed the entry and when.</summary>
    public void Touch(Guid userId, DateTime utcNow)
    {
        LastModifiedByUserId = userId;
        LastModifiedUtc = utcNow;
    }

    /// <summary>
    /// Soft-deletes the entry. A second call throws rather than overwriting the first deletion's
    /// audit fields — callers treat a repeat delete as a no-op before reaching here.
    /// </summary>
    public void MarkDeleted(Guid userId, DateTime utcNow)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException($"Cash entry {Id} is already deleted.");
        }

        IsDeleted = true;
        DeletedUtc = utcNow;
        Touch(userId, utcNow);
    }
}
