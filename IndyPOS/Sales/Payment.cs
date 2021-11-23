namespace IndyPOS.Sales
{
	public class Payment : IPayment
	{
		public int PaymentTypeId { get; set; }

		public decimal Amount { get; set; }

		public int Priority { get; set; }
	}
}
