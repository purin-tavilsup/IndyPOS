namespace IndyPOS.DataAccess.Models
{
	public class PaymentModel
	{
		public int PaymentId { get; set; }

		public int InvoiceId { get; set; }

		public int PaymentTypeId { get; set; }

		public string Amount { get; set; }

		public string DateCreated { get; set; }
	}
}
