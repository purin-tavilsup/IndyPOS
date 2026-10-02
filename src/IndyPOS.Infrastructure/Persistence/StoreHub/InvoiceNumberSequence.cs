namespace IndyPOS.Infrastructure.Persistence.StoreHub;

/// <summary>
/// The one Postgres sequence every bill number comes from. The model (HasSequence and the column
/// default), the sale handler's reservation, and the MigrationTool's setval and verify check all
/// name it through here. The AddInvoiceNumber migration spells the name out on purpose: a
/// migration is frozen history and must not change if this constant ever does.
/// </summary>
public static class InvoiceNumberSequence
{
    public const string Name = "invoice_number_seq";
}
