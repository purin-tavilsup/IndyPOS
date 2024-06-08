namespace IndyPOS.Application.Common.Models;

public class SalesSummary
{
	public decimal InvoiceTotal { get; init; }
	public decimal GeneralProductsTotal { get; init; }
	public decimal HardwareProductsTotal { get; init; }
	public decimal PayLaterPaymentsTotal { get; init; }
	public decimal PayLaterPaymentsTotalForGeneralProducts { get; init; }
	public decimal PayLaterPaymentsTotalForHardwareProducts { get; init; }
	public decimal InvoiceTotalWithoutPayLaterPayments { get; init; }
	public decimal GeneralProductsTotalWithoutPayLaterPayments { get; init; }
	public decimal HardwareProductsTotalWithoutPayLaterPayments { get; init; }
	public decimal CompletedPayLaterPaymentsTotal { get; init; }
	public decimal IncompletePayLaterPaymentsTotal { get; init; }
}