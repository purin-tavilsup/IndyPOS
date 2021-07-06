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

        bool AddProductToSaleInvoice(string barcode);

        bool RemoveProductFromSaleInvoice(string barcode);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);

        bool AddPaymentToSaleInvoice(PaymentType paymentType, decimal paymentAmount);
    }
}
