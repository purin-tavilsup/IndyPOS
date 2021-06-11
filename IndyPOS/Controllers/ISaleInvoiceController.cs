using IndyPOS.Adapters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndyPOS.Controllers
{
    public interface ISaleInvoiceController
    {
        IList<ISaleInvoiceProduct> Products { get; }

        decimal InvoiceTotal { get; }

        void StartNewSaleInvoice();

        bool AddProductToSaleInvoice(string barcode);

        bool RemoveProductFromSaleInvoice(string barcode);

        IInventoryProduct GetInventoryProductByBarcode(string barcode);
    }
}
