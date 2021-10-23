using IndyPOS.Enums;
using IndyPOS.Inventory;
using IndyPOS.SaleInvoice;
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

        bool AddProduct(string barcode);

        bool RemoveProduct(string barcode);

        bool AddPayment(PaymentType paymentType, decimal paymentAmount);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        bool UpdateProductQuantity(string barcode, int quantity);
    }
}
