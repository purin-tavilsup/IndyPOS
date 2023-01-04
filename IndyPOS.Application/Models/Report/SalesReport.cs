using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Models.Report;

[ExcludeFromCodeCoverage]
public class SalesReport
{
	public string Id { get; set; }
	public SalesSummary DaySummary { get; set; }
	public SalesSummary MonthSummary { get; set; }
	public SalesSummary YearSummary { get; set; }
	public DateTime LastUpdateDateTime { get; set; }
}