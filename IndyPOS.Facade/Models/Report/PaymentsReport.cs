using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models.Report
{
	[ExcludeFromCodeCoverage]
    public class PaymentsReport
    {
		public string Id { get; set; }
		public PaymentsSummary DaySummary { get; set; }
		public PaymentsSummary MonthSummary { get; set; }
		public PaymentsSummary YearSummary { get; set; }
		public DateTime LastUpdateDateTime { get; set; }
    }
}
