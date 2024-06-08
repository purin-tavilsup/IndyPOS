using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Common.Extensions;

public static class PaymentsSummaryExtensions
{
	public static PaymentsReport ToReport(this PaymentsSummary summary, Guid id, DateTime created, int referenceId)
	{
		return new PaymentsReport
		{
			Id = id,
			Created = created,
			ReferenceId = referenceId,
			MoneyTransferTotal = summary.MoneyTransferTotal,
			FiftyFiftyTotal = summary.FiftyFiftyTotal,
			M33WeLoveTotal = summary.M33WeLoveTotal,
			WeWinTotal = summary.WeWinTotal,
			WelfareCardTotal = summary.WelfareCardTotal,
			PayLaterTotal = summary.PayLaterTotal
		};
	}
}