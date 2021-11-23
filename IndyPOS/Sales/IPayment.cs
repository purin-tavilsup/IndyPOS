namespace IndyPOS.Sales
{
	public interface IPayment
	{
		int PaymentTypeId { get; set; }

		int Priority { get; set; }

		decimal Amount { get; set; }
	}
}
