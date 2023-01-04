using System.Diagnostics.CodeAnalysis;
using IndyPOS.Application.Interfaces;

namespace IndyPOS.Application.Models;

[ExcludeFromCodeCoverage]
public class Payment : IPayment
{
	public int PaymentTypeId { get; set; }

	public decimal Amount { get; set; }

	public int Priority { get; set; }

	public string Note { get; set; }
}