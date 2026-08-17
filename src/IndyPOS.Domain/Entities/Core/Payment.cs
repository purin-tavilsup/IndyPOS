namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core payment entity for StoreHub.
/// Method is stored as string (no lookup table needed).
/// </summary>
public class Payment
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Method { get; set; } = default!;  // Cash, Card, PayLater, etc.
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// The legacy SQLite PaymentId this row was migrated from, or <c>null</c> for a row v4 created
    /// itself. Defect 8: without it a migrated row cannot be reconciled against its source, and for
    /// invoices there is no natural key to fall back on.
    /// </summary>
    /// <remarks>
    /// Nullable rather than 0-when-absent, unlike <c>StoreUser.LegacyUserId</c> which predates this:
    /// a row v4 created has no legacy id, and 0 is a magic value standing in for absence. Nullable
    /// also keeps the unique index honest, because PostgreSQL treats NULLs as distinct.
    /// </remarks>
    public int? LegacyPaymentId { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = default!;
    public PayLater? PayLater { get; set; }
}
