using IndyPOS.DataAccess.Models;
using IndyPOS.SaleInvoice;

namespace IndyPOS.Adapters
{
	public class SaleInvoiceProductAdapter : ISaleInvoiceProduct
    {
        private InvoiceProduct _adaptee;

        public SaleInvoiceProductAdapter(InvoiceProduct adaptee)
        {
            _adaptee = adaptee;
        }

        public int InvoiceProductId
        {
            get => _adaptee.InvoiceProductId;
            set => _adaptee.InvoiceProductId = value;
        }

        public int InvoiceId
        {
            get => _adaptee.InvoiceId;
            set => _adaptee.InvoiceId = value;
        }

        public int InventoryProductId
        {
            get => _adaptee.InventoryProductId;
            set => _adaptee.InventoryProductId = value;
        }

        public string SKU
        {
            get => _adaptee.SKU;
            set => _adaptee.SKU = value;
        }

        public string Barcode
        {
            get => _adaptee.Barcode;
            set => _adaptee.Barcode = value;
        }

        public string Description
        {
            get => _adaptee.Description;
            set => _adaptee.Description = value;
        }

        public string Manufacturer
        {
            get => _adaptee.Manufacturer;
            set => _adaptee.Manufacturer = value;
        }

        public string Brand
        {
            get => _adaptee.Brand;
            set => _adaptee.Brand = value;
        }

        public int Category
        {
            get => _adaptee.Category;
            set => _adaptee.Category = value;
        }

        public decimal? UnitCost
        {
            get => _adaptee.UnitCost;
            set => _adaptee.UnitCost = value;
        }

        public decimal UnitPrice
        {
            get => _adaptee.UnitPrice;
            set => _adaptee.UnitPrice = value;
        }

        public int Quantity
        {
            get => _adaptee.Quantity;
            set => _adaptee.Quantity = value;
        }

        public string DateCreated
        {
            get => _adaptee.DateCreated;
            set => _adaptee.DateCreated = value;
        }
    }
}
