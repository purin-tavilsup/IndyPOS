using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Application.Models.Report;

[ExcludeFromCodeCoverage]
public class PayLaterPaymentsReport
{
	public string Id { get; set; }
	public PayLaterPaymentsSummary DaySummary { get; set; }
	public PayLaterPaymentsSummary MonthSummary { get; set; }
	public PayLaterPaymentsSummary YearSummary { get; set; }
	public DateTime LastUpdateDateTime { get; set; }
}