using IndyPOS.Adapters;

namespace IndyPOS.SaleInvoice
{
	public class Payment : IPayment
	{
		public int PaymentTypeId { get; set; }

		public decimal Amount { get; set; }
	}
}
