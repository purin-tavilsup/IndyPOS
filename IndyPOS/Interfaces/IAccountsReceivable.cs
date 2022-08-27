namespace IndyPOS.Interfaces
{
    public interface IAccountsReceivable
    {
		int PaymentId { get; }

		string Description { get; }

		int InvoiceId { get; }

		decimal ReceivableAmount { get; }

		decimal PaidAmount { get; set; }

		bool IsCompleted { get; set; }

		string DateCreated { get; }

		string DateUpdated { get; }
	}
}
