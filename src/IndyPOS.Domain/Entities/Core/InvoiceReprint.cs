namespace IndyPOS.Domain.Entities.Core;

/// <summary>
/// One reprint REQUESTED for a bill. Append-only and immutable: a printer failure afterwards leaves
/// the row in place, and pressing reprint again adds a second — the owner sees both attempts.
/// </summary>
public class InvoiceReprint
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string StoreId { get; set; } = default!;

    /// <summary>When the reprint was requested.</summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>Equals CreatedUtc: kept for the repo-wide entity convention; a reprint never changes.</summary>
    public DateTime LastModifiedUtc { get; set; }

    /// <summary>Who reprinted — from the token, never the request body.</summary>
    public Guid CreatedByUserId { get; set; }
}
