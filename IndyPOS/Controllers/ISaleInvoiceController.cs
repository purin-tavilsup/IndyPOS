using IndyPOS.Enums;
using IndyPOS.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Controllers
{
    public interface ISaleInvoiceController
    {
        IReadOnlyCollection<ISaleInvoiceProduct> Products { get; }

        IReadOnlyCollection<IPayment> Payments { get; }

        decimal InvoiceTotal { get; }

        decimal PaymentTotal { get; }

		decimal BalanceRemaining { get; }

		bool IsRefundInvoice { get; }

        bool IsPendingPayment { get; }

		decimal Changes { get; }

        void StartNewSale();

		IList<string> ValidateSaleInvoice();

        Task CompleteSale();

		void PrintReceipt();

        void RemoveAllPayments();

        void AddProduct(InventoryProductModel product);

		void AddProduct(InventoryProductModel product, decimal unitPrice, int quantity, string note);

        void RemoveProduct(ISaleInvoiceProduct product);

        void AddPayment(PaymentType paymentType, decimal paymentAmount, string note);

        void UpdateProductQuantity(int inventoryProductId, int priority, int quantity);

		void UpdateProductUnitPrice(int inventoryProductId, int priority, decimal unitPrice, string note);

		InventoryProductModel GetInventoryProductByBarcode(string barcode);
	}
}
