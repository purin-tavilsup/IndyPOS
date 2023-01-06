using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Windows.Forms.Interfaces;

namespace IndyPOS.Windows.Forms.Controllers
{
    public class SaleInvoiceController : ISaleInvoiceController
    {
		private readonly ISaleInvoiceHelper _saleInvoiceHelper;
		private readonly IReportHelper _reportHelper;

        public SaleInvoiceController(ISaleInvoiceHelper saleInvoiceHelper,
									 IReportHelper reportHelper)
        {
			_saleInvoiceHelper = saleInvoiceHelper;
			_reportHelper = reportHelper;
		}
		
        public void StartNewSale()
		{
			_saleInvoiceHelper.StartNewSale();
        }

        public void RemoveAllPayments()
		{
			_saleInvoiceHelper.RemoveAllPayments();
		}

		public void AddProduct(IInventoryProduct product)
		{
			_saleInvoiceHelper.AddProduct(product);
		}

		public void AddProduct(IInventoryProduct product, decimal unitPrice, int quantity, string note)
        {
			_saleInvoiceHelper.AddProduct(product, unitPrice, quantity, note);
        }

		public ISaleInvoiceProduct GetSaleInvoiceProduct(string barcode, int priority)
		{
			return _saleInvoiceHelper.GetSaleInvoiceProduct(barcode, priority);
		}

        public void RemoveProduct(ISaleInvoiceProduct product)
        {
			_saleInvoiceHelper.RemoveProduct(product);
        }

        public IInventoryProduct GetInventoryProductByBarcode(string barcode)
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

		public IInvoiceInfo GetInvoiceInfo()
		{
			return _saleInvoiceHelper.GetInvoiceInfo();
		}

		public async Task CompleteSaleAsync()
		{
			var invoiceInfo = _saleInvoiceHelper.CompleteSale();

			await _reportHelper.UpdateReportAsync(invoiceInfo);
		}

		public void PrintReceipt()
		{
			_saleInvoiceHelper.PrintReceipt();
		}

		public bool IsRefundInvoice()
		{
			return _saleInvoiceHelper.IsRefundInvoice();
		}

		public bool IsPendingPayment()
		{
			return _saleInvoiceHelper.IsPendingPayment();
		}

		public decimal CalculateInvoiceTotal()
		{
			return _saleInvoiceHelper.CalculateInvoiceTotal();
		}

		public decimal CalculatePaymentTotal()
		{
			return _saleInvoiceHelper.CalculatePaymentTotal();
		}

		public decimal CalculateBalanceRemaining()
		{
			return _saleInvoiceHelper.CalculateBalanceRemaining();
		}

		public decimal CalculateChanges()
		{
			return _saleInvoiceHelper.CalculateChanges();
		}
	}
}
