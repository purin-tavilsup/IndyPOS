namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// Core invoice entity for StoreHub.
/// Simplified design - no CustomerId, no status (all completed on save).
/// </summary>
public class Invoice
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>
    /// The legacy SQLite InvoiceId this row was migrated from, or <c>null</c> for a row v4 created
    /// itself. Defect 8: without it a migrated row cannot be reconciled against its source, and for
    /// invoices there is no natural key to fall back on.
    /// </summary>
    /// <remarks>
    /// Nullable rather than 0-when-absent, unlike <c>StoreUser.LegacyUserId</c> which predates this:
    /// a row v4 created has no legacy id, and 0 is a magic value standing in for absence. Nullable
    /// also keeps the unique index honest, because PostgreSQL treats NULLs as distinct.
    /// </remarks>
    public int? LegacyInvoiceId { get; set; }

    /// <summary>
    /// The bill number printed on the receipt: per store, running, never reused. Assigned by the
    /// database from <c>invoice_number_seq</c>. A v3 bill keeps its v3 number and v4 carries on
    /// after the store's last one.
    /// </summary>
    /// <remarks>
    /// 0 means "not assigned yet": EF then leaves the column out of the INSERT and the column
    /// default fills it. The same default is what lets the previous release's binaries, which never
    /// mention the column, keep writing sales after an installer rollback.
    /// </remarks>
    public long InvoiceNumber { get; set; }

    // Navigation properties
    public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
