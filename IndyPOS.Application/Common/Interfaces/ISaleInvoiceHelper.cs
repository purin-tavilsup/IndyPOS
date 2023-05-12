using IndyPOS.Application.Common.Enums;

namespace IndyPOS.Application.Common.Interfaces;

public interface ISaleInvoiceHelper
{
	IList<ISaleInvoiceProduct> Products { get; }

	IList<IPayment> Payments { get; }

	void StartNewSale();

	IList<string> ValidateSaleInvoice();

	IInvoiceInfo CompleteSale();

	IInvoiceInfo GetInvoiceInfo();

	bool IsRefundInvoice();

	bool IsPendingPayment();

	decimal CalculateInvoiceTotal();

	decimal CalculatePaymentTotal();

	decimal CalculateBalanceRemaining();

	decimal CalculateChanges();

	void PrintReceipt();

	void RemoveAllPayments();

	void AddProduct(IInventoryProduct product);

	void AddProduct(IInventoryProduct product, decimal unitPrice, int quantity, string note);

	ISaleInvoiceProduct GetSaleInvoiceProduct(string barcode, int priority);

	void RemoveProduct(ISaleInvoiceProduct product);

	void AddPayment(PaymentType paymentType, decimal paymentAmount, string note);

	void UpdateProductQuantity(int inventoryProductId, int priority, int quantity);

	void UpdateProductUnitPrice(int inventoryProductId, int priority, decimal unitPrice, string note);

	IInventoryProduct GetInventoryProductByBarcode(string barcode);

	IEnumerable<IFinalInvoice> GetInvoicesByPeriod(TimePeriod period);

	IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateOnly startDate, DateOnly endDate);

	IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateOnly date);

	IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateOnly startDate, DateOnly endDate);

	IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId);

	IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId);
}