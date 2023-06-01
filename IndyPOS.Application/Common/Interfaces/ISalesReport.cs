namespace IndyPOS.Application.Common.Interfaces;

public interface ISalesReport
{
	public decimal InvoiceTotal { get; }

	public decimal GeneralProductsTotal { get; }

	public decimal HardwareProductsTotal { get; }

	public decimal PayLaterPaymentsTotal { get; }

	public decimal PayLaterPaymentsTotalForGeneralProducts { get; }

	public decimal PayLaterPaymentsTotalForHardwareProducts { get; }

	public decimal InvoiceTotalWithoutPayLaterPayments { get; }

	public decimal GeneralProductsTotalWithoutPayLaterPayments { get; }

	public decimal HardwareProductsTotalWithoutPayLaterPayments { get; }

	public decimal CompletedPayLaterPaymentsTotal { get; }

	public decimal IncompletePayLaterPaymentsTotal { get; }
}