using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.DataAccess.Models;

[ExcludeFromCodeCoverage]
public class PaymentType
{
	public int Id { get; set; }

	public string Type { get; set; } = string.Empty;
}