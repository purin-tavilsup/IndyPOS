using IndyPOS.Common.Enums;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryProductModel = IndyPOS.DataAccess.Models.InventoryProduct;

namespace IndyPOS.Controllers
{
	public class SaleInvoiceController : ISaleInvoiceController
    {
		private readonly ISaleInvoiceHelper _saleInvoiceHelper;

        public IList<ISaleInvoiceProduct> Products => _saleInvoiceHelper.Products;

        public IList<IPayment> Payments => _saleInvoiceHelper.Payments;

        public decimal InvoiceTotal => _saleInvoiceHelper.InvoiceTotal;

        public decimal PaymentTotal => _saleInvoiceHelper.PaymentTotal;

		public decimal BalanceRemaining => _saleInvoiceHelper.BalanceRemaining;

		public bool IsRefundInvoice => _saleInvoiceHelper.IsRefundInvoice;

		public bool IsPendingPayment => IsRefundInvoice
											? _saleInvoiceHelper.InvoiceTotal != _saleInvoiceHelper.PaymentTotal
											: _saleInvoiceHelper.InvoiceTotal > _saleInvoiceHelper.PaymentTotal;

        public decimal Changes => _saleInvoiceHelper.Changes;

        public SaleInvoiceController(ISaleInvoiceHelper saleInvoiceHelper)
        {
			_saleInvoiceHelper = saleInvoiceHelper;
		}
		
        public void StartNewSale()
		{
			_saleInvoiceHelper.StartNewSale();
        }

        public void RemoveAllPayments()
		{
			_saleInvoiceHelper.RemoveAllPayments();
		}

		public void AddProduct(InventoryProductModel product)
		{
			_saleInvoiceHelper.AddProduct(product);
		}

		public void AddProduct(InventoryProductModel product, decimal unitPrice, int quantity, string note)
        {
			_saleInvoiceHelper.AddProduct(product, unitPrice, quantity, note);
        }

        public void RemoveProduct(ISaleInvoiceProduct product)
        {
			_saleInvoiceHelper.RemoveProduct(product);
        }

        public InventoryProductModel GetInventoryProductByBarcode(string barcode)
		{
			return _saleInvoiceHelper.GetInventoryProductByBarcode(barcode);
        }

        public void AddPayment(PaymentType paymentType, decimal paymentAmount, string note)
		{
			_saleInvoiceHelper.AddPayment(paymentType, paymentAmount, note);
        }

		public void UpdateProductUnitPrice(int inventoryProductId, int priority, decimal unitPrice, string note)
		{
			_saleInvoiceHelper.UpdateProductUnitPrice(inventoryProductId, priority, unitPrice, note);
		}

        public void UpdateProductQuantity(int inventoryProductId, int priority, int newQuantity)
		{
			_saleInvoiceHelper.UpdateProductQuantity(inventoryProductId, priority, newQuantity);
		}

		public IList<string> ValidateSaleInvoice()
		{
			return _saleInvoiceHelper.ValidateSaleInvoice();
		}

		public async Task CompleteSale()
		{
			await _saleInvoiceHelper.CompleteSale();
		}

		public void PrintReceipt()
		{
			_saleInvoiceHelper.PrintReceipt();
		}
	}
}
