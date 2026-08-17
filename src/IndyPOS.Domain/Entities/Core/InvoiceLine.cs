namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core invoice line entity for StoreHub.
/// ProductName is a snapshot for historical accuracy.
/// </summary>
/// <remarks>
/// The last three are nullable and carry legacy detail the v3 till recorded that nothing else in v4
/// holds (defect 6). They are absent, not zero, when the till did not record them: the legacy
/// defaults are 0 and empty string, and 0 is not a group price or a position on an invoice.
/// <para>
/// Deliberately NOT here, both measured across all three real stores rather than assumed:
/// <c>OriginalUnitPrice</c>, which is always either 0 or exactly <see cref="UnitPrice"/> and so
/// records no discount anywhere, and <c>IsGroupProduct</c>, set on 15 rows in one store and never
/// where <c>GroupPrice</c> is actually present.
/// </para>
/// </remarks>
public class InvoiceLine
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Free text the till captured against this line. Two real uses, both confirmed by the shop
    /// owner: the name of something sold that was not in the catalogue, and a cashier's remark about
    /// the sale. Kept as one neutral field because the legacy column does not distinguish them.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>The line's position on its invoice. Exactly 1..n on 99% of real invoices.</summary>
    public int? Priority { get; set; }

    /// <summary>The group price this line was sold at, when it was part of a group sale.</summary>
    public decimal? GroupPrice { get; set; }

    /// <summary>
    /// The legacy SQLite InvoiceProductId this row was migrated from, or <c>null</c> for a row v4 created
    /// itself. Defect 8: without it a migrated row cannot be reconciled against its source, and for
    /// invoices there is no natural key to fall back on.
    /// </summary>
    /// <remarks>
    /// Nullable rather than 0-when-absent, unlike <c>StoreUser.LegacyUserId</c> which predates this:
    /// a row v4 created has no legacy id, and 0 is a magic value standing in for absence. Nullable
    /// also keeps the unique index honest, because PostgreSQL treats NULLs as distinct.
    /// </remarks>
    public int? LegacyInvoiceLineId { get; set; }

    // Navigation properties
    public Invoice Invoice { get; set; } = default!;
    public Product Product { get; set; } = default!;

    // Calculated property (no DB storage)
    public decimal LineTotal => UnitPrice * Quantity;
}
