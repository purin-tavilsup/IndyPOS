using IndyPOS.Adapters;
using IndyPOS.Enums;
using System.Collections.Generic;

namespace IndyPOS.Controllers
{
	public interface ISaleInvoiceController
    {
        IList<ISaleInvoiceProduct> Products { get; }

        IList<IPayment> Payments { get; }

        decimal InvoiceTotal { get; }

        decimal PaymentTotal { get; }

        decimal Changes { get; }

        void StartNewSale();

        void ClearProductsAndPayments();

        bool AddProduct(string barcode);

        bool RemoveProduct(string barcode);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        bool AddPayment(PaymentType paymentType, decimal paymentAmount);

        bool UpdateProductQuantity(string barcode, int quantity);
    }
}
