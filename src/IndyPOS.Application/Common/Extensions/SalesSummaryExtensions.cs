using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Common.Extensions;

public static class SalesSummaryExtensions
{
	public static SalesReport ToReport(this SalesSummary summary, Guid id, DateTime created, int referenceId)
	{
		return new SalesReport
		{
			Id = id,
			Created = created,
			ReferenceId = referenceId,
			InvoiceTotal = summary.InvoiceTotal,
			GeneralProductsTotal = summary.GeneralProductsTotal,
			HardwareProductsTotal = summary.HardwareProductsTotal,
			PayLaterPaymentsTotal = summary.PayLaterPaymentsTotal,
			PayLaterPaymentsTotalForGeneralProducts = summary.PayLaterPaymentsTotalForGeneralProducts,
			PayLaterPaymentsTotalForHardwareProducts = summary.PayLaterPaymentsTotalForHardwareProducts,
			InvoiceTotalWithoutPayLaterPayments = summary.InvoiceTotalWithoutPayLaterPayments,
			GeneralProductsTotalWithoutPayLaterPayments = summary.GeneralProductsTotalWithoutPayLaterPayments,
			HardwareProductsTotalWithoutPayLaterPayments = summary.HardwareProductsTotalWithoutPayLaterPayments,
			CompletedPayLaterPaymentsTotal = summary.CompletedPayLaterPaymentsTotal,
			IncompletePayLaterPaymentsTotal = summary.IncompletePayLaterPaymentsTotal
		};
	}
}