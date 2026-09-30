namespace IndyPOS.Infrastructure.Persistence.StoreHub;

/// <summary>
/// The one Postgres sequence every bill number comes from: the column default, the sale handler's
/// reservation, the migration backfill and the MigrationTool's setval all name it through here.
/// </summary>
public static class InvoiceNumberSequence
{
    public const string Name = "invoice_number_seq";
}
