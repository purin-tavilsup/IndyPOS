using System.Diagnostics.CodeAnalysis;

namespace IndyPOS.Facade.Models.Report;

[ExcludeFromCodeCoverage]
public class PaymentsSummary
{
	public double MoneyTransferTotal { get; set; }

	public double FiftyFiftyTotal { get; set; }

	public double M33WeLoveTotal { get; set; }

	public double WeWinTotal { get; set; }

	public double WelfareCardTotal { get; set; }

	public double ArTotal { get; set; }
}