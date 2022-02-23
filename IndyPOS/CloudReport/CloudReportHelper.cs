using IndyPOS.Controllers;
using IndyPOS.Mqtt;
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
		private readonly IMqttClient _mqttClient;
		private readonly IReportController _reportController;
		private readonly ILogger _logger;

		public CloudReportHelper(IMqttClient mqttClient,
								 IReportController reportController)
		{
			_mqttClient = mqttClient;
			_reportController = reportController;
			_logger = Log.Logger;
		}

		public async Task PublishSaleReport(int invoiceId)
		{
			try
			{
				var topic = ConfigurationManager.AppSettings.Get("InvoiceReportTopic");

				var report = await CreateSaleReport(invoiceId);

				await _mqttClient.Publish(topic, JsonSerializer.Serialize(report));
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Error occurred while publishing a sale report to cloud.");
			}
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
