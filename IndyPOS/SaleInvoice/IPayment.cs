namespace IndyPOS.SaleInvoice
{
	public interface IPayment
	{
		int PaymentTypeId { get; set; }

		decimal Amount { get; set; }
	}
}
