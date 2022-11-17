using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models.Report;

[ExcludeFromCodeCoverage]
public class ArReport
{
	public string Id { get; set; }
	public ArSummary DaySummary { get; set; }
	public ArSummary MonthSummary { get; set; }
	public ArSummary YearSummary { get; set; }
	public DateTime LastUpdateDateTime { get; set; }
}