using IndyPOS.Facade.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models
{
	[ExcludeFromCodeCoverage]
	public class Payment : IPayment
	{
		public int PaymentTypeId { get; set; }

		public decimal Amount { get; set; }

		public int Priority { get; set; }

		public string Note { get; set; }
	}
}
