using IndyPOS.Application.Common.Models;

namespace IndyPOS.Application.Common.Interfaces;

public interface IInvoiceInfo
{
	/// <summary>
	/// Legacy SQLite invoice ID.
	/// </summary>
	int Id { get; }

	/// <summary>
	/// StoreHub invoice ID (UUID). Null for legacy SQLite-only invoices.
	/// </summary>
	Guid? StoreHubInvoiceId { get; }

	IList<Product> Products { get; }

	IList<Payment> Payments { get; }

	bool IsRefundInvoice { get; }

	decimal InvoiceTotal { get; }

	decimal PaymentTotal { get; }

	decimal Changes { get; }

	bool HasPayLaterPayment { get; }
}