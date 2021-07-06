namespace IndyPOS.Adapters
{
	public interface IPayment
	{
		int PaymentTypeId { get; set; }

		decimal Amount { get; set; }
	}
}
