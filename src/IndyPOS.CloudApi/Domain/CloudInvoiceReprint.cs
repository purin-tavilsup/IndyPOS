namespace IndyPOS.CloudApi.Domain;

/// <summary>
/// One reprint of a bill, mirrored from the store's invoice_reprint row by InvoiceReprinted.
/// Insert-only: a reprint is an audit fact, so nothing updates or deletes it. It means a reprint
/// was requested; a printer failure afterwards does not remove it.
/// </summary>
public class CloudInvoiceReprint
{
    /// <summary>The store's reprint id, so a row can be traced back to the till.</summary>
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime SyncedAtUtc { get; set; }
}
