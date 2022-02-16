using IndyPOS.Controllers;
using IndyPOS.Enums;
using IndyPOS.Mqtt;
using IndyPOS.Sales;
using System.Threading.Tasks;

namespace IndyPOS.CloudReport
{
    internal class CloudReportHelper : ICloudReportHelper
	{
		private readonly IMqttClient _mqttClient;
		private readonly IReportController _reportController;

		private const string InvoicesTotalTopic = "RungratPOS/1/InvoicesTotal";
		private const string InvoicesTotalWithoutArTopic = "RungratPOS/1/InvoicesTotalWithoutAr";
		private const string GeneralProductsTotalTopic = "RungratPOS/1/GeneralProductsTotal";
		private const string GeneralProductsTotalWithoutArTopic = "RungratPOS/1/GeneralProductsTotalWithoutAr";
		private const string HardwareProductsTotalTopic = "RungratPOS/1/HardwareProductsTotal";
		private const string HardwareProductsTotalWithoutArTopic = "RungratPOS/1/HardwareProductsTotalWithoutAr";
		
		private const string ArTotalTopic = "RungratPOS/1/ARTotal";
		private const string CompletedArTotalTopic = "RungratPOS/1/CompletedARTotal";
		private const string IncompleteArTotalTopic = "RungratPOS/1/IncompleteARTotal";
		
		private const string CashTotalTopic = "RungratPOS/1/Payment/CashTotal";
		private const string MoneyTransferTotalTopic = "RungratPOS/1/Payment/MoneyTransferTotal";
		private const string FiftyFiftyTotalTopic = "RungratPOS/1/Payment/FiftyFiftyTotal";
		private const string M33WeLoveTotalTopic = "RungratPOS/1/Payment/M33WeLoveTotal";
		private const string WeWinTotalTopic = "RungratPOS/1/Payment/WeWinTotal";
		private const string WelfareCardTotalTopic = "RungratPOS/1/Payment/WelfareCardTotal";
		private const string ArPaymentTotalTopic = "RungratPOS/1/Payment/ArTotal";

        public CloudReportHelper(IMqttClient mqttClient,
								 IReportController reportController)
		{
			_mqttClient = mqttClient;
			_reportController = reportController;
		}

		public async Task PublishToCloud()
		{
			_reportController.LoadInvoicesByPeriod(ReportPeriod.ThisMonth);

			await PublishSaleReport();
			await PublishPaymentReport();
			await PublishArReport();
		}

		private async Task PublishSaleReport()
        {
			var saleReport = await GetSaleReportAsync();

			await _mqttClient.Publish(InvoicesTotalTopic, $"{saleReport.InvoicesTotal:N}");
			await _mqttClient.Publish(InvoicesTotalWithoutArTopic, $"{saleReport.InvoicesTotalWithoutAr:N}");
			await _mqttClient.Publish(GeneralProductsTotalTopic, $"{saleReport.GeneralProductsTotal:N}");
			await _mqttClient.Publish(GeneralProductsTotalWithoutArTopic, $"{saleReport.GeneralProductsTotalWithoutAr:N}");
			await _mqttClient.Publish(HardwareProductsTotalTopic, $"{saleReport.HardwareProductsTotal:N}");
			await _mqttClient.Publish(HardwareProductsTotalWithoutArTopic, $"{saleReport.HardwareProductsTotalWithoutAr:N}");
		}

		private async Task PublishPaymentReport()
        {
			var paymentReport = await GetPaymentReportAsync();

			await _mqttClient.Publish(CashTotalTopic, $"{paymentReport.CashTotal:N}");
			await _mqttClient.Publish(MoneyTransferTotalTopic, $"{paymentReport.MoneyTransferTotal:N}");
			await _mqttClient.Publish(FiftyFiftyTotalTopic, $"{paymentReport.FiftyFiftyTotal:N}");
			await _mqttClient.Publish(M33WeLoveTotalTopic, $"{paymentReport.M33WeLoveTotal:N}");
			await _mqttClient.Publish(WeWinTotalTopic, $"{paymentReport.WeWinTotal:N}");
			await _mqttClient.Publish(WelfareCardTotalTopic, $"{paymentReport.WelfareCardTotal:N}");
			await _mqttClient.Publish(ArPaymentTotalTopic, $"{paymentReport.ArTotal:N}");
        }

		private async Task PublishArReport()
        {
			var arReport = await GetArReportAsync();

			await _mqttClient.Publish(ArTotalTopic, $"{arReport.ArTotal:N}");
			await _mqttClient.Publish(CompletedArTotalTopic, $"{arReport.CompletedArTotal:N}");
			await _mqttClient.Publish(IncompleteArTotalTopic, $"{arReport.IncompleteArTotal:N}");
        }

		private async Task<SaleReport> GetSaleReportAsync()
		{
			return await Task.Run(_reportController.CreateSaleReport);
		}

		private async Task<PaymentReport> GetPaymentReportAsync()
		{
			return await Task.Run(_reportController.CreatePaymentReport);
		}

		private async Task<ArReport> GetArReportAsync()
		{
			return await Task.Run(_reportController.CreateArReport);
		}
	}
}
