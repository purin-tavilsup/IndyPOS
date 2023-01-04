using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Models.Report;

[ExcludeFromCodeCoverage]
public class Invoice
{
	public string Id { get; set; }
	public string Date { get; set; }
	public DateTime DateTime { get; set; }
	public Product[] Products { get; set; }
	public Payment[] Payments { get; set; }
}