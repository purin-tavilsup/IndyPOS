namespace IndyPOS.Sales
{
    public interface IFinalInvoice
    {
		int InvoiceId { get; }

		decimal Total { get; }

		int? CustomerId { get; }

		int UserId { get; }

		string DateCreated { get; }
    }
}
