using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.InventoryProducts;

namespace IndyPOS.Application.Common.Interfaces;

public interface ISaleService
{
	IList<Product> Products { get; }

	IList<Payment> Payments { get; }

	void StartNewSale();

	IList<string> ValidateSaleInvoice();

	Task<IInvoiceInfo> CompleteSaleAsync();

	bool IsRefundInvoice();

	bool IsPendingPayment();

	decimal CalculateInvoiceTotal();

	decimal CalculatePaymentTotal();

	decimal CalculateBalanceRemaining();

	decimal CalculateChanges();

	void RemoveAllPayments();

	Product GetSaleInvoiceProduct(string barcode, int priority);

	Task<InventoryProductDto> GetInventoryProductByBarcodeAsync(string barcode);

	void AddProduct(InventoryProductDto product);

	void AddProduct(InventoryProductDto product, decimal unitPrice, int quantity, string note);

	void RemoveProduct(Product product);

	void AddPayment(PaymentType paymentType, decimal paymentAmount, string note);

	Task UpdateProductQuantityAsync(int inventoryProductId, int priority, int quantity);

	void UpdateProductUnitPrice(int inventoryProductId, int priority, decimal unitPrice, string note);
}