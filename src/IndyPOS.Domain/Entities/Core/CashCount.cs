using IndyPOS.Domain.ValueObjects;

namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// One count of the drawer's notes and coins. Append-only: every count is kept for audit, and
/// only the latest of the day feeds the cash difference.
/// </summary>
public class CashCount
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
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

    /// <summary>When the count was taken.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Equals CreatedUtc: kept for the repo-wide entity convention; a count never changes.</summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>Who counted.</summary>
    public Guid CreatedByUserId { get; set; }

    public decimal CountedTotal => CashDenominations.Total(
    [
        BankNote1000Count, BankNote500Count, BankNote100Count, BankNote50Count, BankNote20Count,
        Coin10Count, Coin5Count, Coin2Count, Coin1Count
    ]);
}
