using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models.Report
{
	[ExcludeFromCodeCoverage]
    public class ArSummary
    {
		public double ArTotal { get; set; }
		public double CompletedArTotal { get; set; }
		public double IncompleteArTotal { get; set; }
    }
}
