using IndyPOS.Controllers;
using IndyPOS.Sales;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace IndyPOS.CloudReport
{
    internal class CloudReportHelper : ICloudReportHelper
	{
		private readonly IReportController _reportController;
		private readonly ILogger _logger;

		public CloudReportHelper(IReportController reportController)
		{
			_reportController = reportController;
			_logger = Log.Logger;
		}

		private async Task<Invoice> CreateSaleReport(int invoiceId)
        {
            var invoice = await GetInvoiceByInvoiceId(invoiceId);
            var products = await GetProductsByInvoiceId(invoiceId);
            var payments = await GetPaymentsByInvoiceId(invoiceId);

            return new Invoice
            {
                InvoiceId = invoiceId,
                Products = products,
                Payment = payments,
                DateCreated = invoice.DateCreated
            };
        }

        private async Task<IFinalInvoice> GetInvoiceByInvoiceId(int invoiceId)
		{
			return await Task.Run(() => _reportController.GetInvoiceByInvoiceId(invoiceId));
		}

		private async Task<IEnumerable<Product>> GetProductsByInvoiceId(int invoiceId)
		{
			var products = await Task.Run(() => _reportController.GetInvoiceProductsByInvoiceId(invoiceId));

			return products.Select(x => new Product
										{
											Id = x.InvoiceProductId,
											Barcode = x.Barcode,
											Category = x.Category,
											Total = x.UnitPrice * x.Quantity
										});
		}

		private async Task<IEnumerable<Payment>> GetPaymentsByInvoiceId(int invoiceId)
		{
			var payments = await Task.Run(() => _reportController.GetPaymentsByInvoiceId(invoiceId));

			return payments.Select(x => new Payment
										{
											Id = x.PaymentId,
											Type = x.PaymentTypeId,
											Amount = x.Amount
										});
		}
	}
}
