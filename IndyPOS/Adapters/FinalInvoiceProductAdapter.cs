using IndyPOS.DataAccess.Models;
using IndyPOS.Sales;

namespace IndyPOS.Adapters
{
	public class FinalInvoiceProductAdapter : IFinalInvoiceProduct
	{
		private readonly InvoiceProduct _adaptee;

		public FinalInvoiceProductAdapter(InvoiceProduct adaptee)
		{
			_adaptee = adaptee;
		}

		public int InvoiceProductId => _adaptee.InvoiceProductId;

		public int Priority => _adaptee.Priority;

		public int InvoiceId => _adaptee.InvoiceId;

		public int InventoryProductId => _adaptee.InventoryProductId;

		public string Barcode => _adaptee.Barcode;

		public string Description => _adaptee.Description;

		public string Manufacturer => _adaptee.Manufacturer;

		public string Brand => _adaptee.Brand;

		public int Category => _adaptee.Category;

		public decimal UnitPrice => _adaptee.UnitPrice;

		public int Quantity => _adaptee.Quantity;

		public bool IsTrackable => _adaptee.IsTrackable;

		public string DateCreated => _adaptee.DateCreated;

		public string Note => _adaptee.Note;
	}
}
