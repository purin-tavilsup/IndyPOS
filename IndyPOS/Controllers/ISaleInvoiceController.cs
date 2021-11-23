using IndyPOS.Enums;
using IndyPOS.Inventory;
using IndyPOS.Sales;
using System.Collections.Generic;

namespace IndyPOS.Controllers
{
	public interface ISaleInvoiceController
    {
        IReadOnlyCollection<ISaleInvoiceProduct> Products { get; }

        IReadOnlyCollection<IPayment> Payments { get; }

        decimal InvoiceTotal { get; }

        decimal PaymentTotal { get; }

        decimal Changes { get; }

        void StartNewSale();

		IList<string> ValidateSaleInvoice();

        void CompleteSale();

        void RemoveAllPayments();

        void AddProduct(string barcode);

        void RemoveProduct(ISaleInvoiceProduct product);

        void AddPayment(PaymentType paymentType, decimal paymentAmount);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        void UpdateProductQuantity(int inventoryProductId, int priority, int quantity);
    }
}
