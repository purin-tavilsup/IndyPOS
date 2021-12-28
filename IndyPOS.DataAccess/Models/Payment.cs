using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models
{
	[ExcludeFromCodeCoverage]
	public class Payment
	{
		public int PaymentId { get; set; }

		public int InvoiceId { get; set; }

		public int PaymentTypeId { get; set; }

		public decimal Amount { get; set; }

		public string DateCreated { get; set; }
	}
}
