using IndyPOS.Common.Enums;
using IndyPOS.Facade.Interfaces;

namespace IndyPOS.Interfaces
{
	public interface ISaleInvoiceController
    {
		void StartNewSale();

		IList<string> ValidateSaleInvoice();

        Task CompleteSale();

		IInvoiceInfo GetInvoiceInfo();

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

		bool IsRefundInvoice();

		bool IsPendingPayment();

		decimal CalculateInvoiceTotal();

		decimal CalculatePaymentTotal();

		decimal CalculateBalanceRemaining();

		decimal CalculateChanges();
	}
}
