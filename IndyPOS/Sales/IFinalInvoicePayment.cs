namespace IndyPOS.Sales
{
    public interface IFinalInvoicePayment
    {
		int PaymentId { get; }

		int InvoiceId { get; }

		int PaymentTypeId { get; }

		decimal Amount { get; }

		string DateCreated { get; }

    }
}
