using IndyPOS.Common.Interfaces;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace IndyPOS.Facade.Helpers
{
	public class ReportHelper : IReportHelper
	{
		private readonly string _reportsDirectory;
		private readonly IJsonUtility _jsonUtility;
		private readonly ILogger _logger; 

		public ReportHelper(IConfiguration configuration, ILogger logger, IJsonUtility jsonUtility)
        {
			_logger = logger;
			_reportsDirectory = configuration.ReportsDirectory;
			_jsonUtility = jsonUtility;
        }

		public async Task<SalesReport> UpdateReport(SalesSummary summary)
		{
			var today = DateTime.Now;
			var filePath = $"{_reportsDirectory}\\report-{today.Year}.json";
			var report = await GetReportFromFile(filePath, today);

			report.YearSummary = GetYearSummary(report, summary, today);
			report.MonthSummary = GetMonthSummary(report, summary, today);
			report.DaySummary = GetDaySummary(report, summary, today);
			report.LastUpdateDateTime = today;

			await SaveReportToFile(report, filePath);

			return report;
		}

		private static SalesSummary GetYearSummary(SalesReport report, SalesSummary summary, DateTime today)
		{
			var resetRequired = report.LastUpdateDateTime.Year != today.Year;

			return resetRequired ? summary : UpdateSummary(report.YearSummary, summary);
		}

		private static SalesSummary GetMonthSummary(SalesReport report, SalesSummary summary, DateTime today)
		{
			var resetRequired = report.LastUpdateDateTime.ToString("MM-yyyy") != today.ToString("MM-yyyy");

			return resetRequired ? summary : UpdateSummary(report.MonthSummary, summary);
		}

		private static SalesSummary GetDaySummary(SalesReport report, SalesSummary summary, DateTime today)
        {
			var resetRequired = report.LastUpdateDateTime.ToString("MM-dd-yyyy") != today.ToString("MM-dd-yyyy");

			return resetRequired ? summary : UpdateSummary(report.DaySummary, summary);
		}

		private static SalesSummary UpdateSummary(SalesSummary previousSummary, SalesSummary newSummary)
        {
			return new SalesSummary
			{
				InvoiceTotal = previousSummary.InvoiceTotal + newSummary.InvoiceTotal,
				GeneralProductsTotal = previousSummary.GeneralProductsTotal + newSummary.GeneralProductsTotal,
				HardwareProductsTotal = previousSummary.HardwareProductsTotal + newSummary.HardwareProductsTotal,
				ArTotal = previousSummary.ArTotal + newSummary.ArTotal,
				ArTotalForGeneralProducts = previousSummary.ArTotalForGeneralProducts + newSummary.ArTotalForGeneralProducts,
				ArTotalForHardwareProducts = previousSummary.ArTotalForHardwareProducts + newSummary.ArTotalForHardwareProducts,
				InvoiceTotalWithoutAr = previousSummary.InvoiceTotalWithoutAr + newSummary.InvoiceTotalWithoutAr,
				GeneralProductsTotalWithoutAr = previousSummary.GeneralProductsTotalWithoutAr + newSummary.GeneralProductsTotalWithoutAr,
				HardwareProductsTotalWithoutAr = previousSummary.HardwareProductsTotalWithoutAr + newSummary.HardwareProductsTotalWithoutAr
			};
        }

		private async Task SaveReportToFile(SalesReport report, string filePath)
		{
			try
			{
				await _jsonUtility.SaveToFileAsync(report, filePath);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to save report to file.");
				throw;
			}
		}

		private async Task<SalesReport> GetReportFromFile(string filePath, DateTime today)
		{
			try
			{
				if (!File.Exists(filePath))
                {
					var report = CreateNewReport(today);

					await SaveReportToFile(report, filePath);
				}

				return await _jsonUtility.ReadFromFileAsync<SalesReport>(filePath);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to get report from file.");
				throw;
			}
		}

		private static SalesReport CreateNewReport(DateTime today)
        {
			return new SalesReport
			{
				Id = $"{today.Year}",
				DaySummary = new SalesSummary(),
				MonthSummary = new SalesSummary(),
				YearSummary = new SalesSummary(),
				LastUpdateDateTime = today
			};
        }
    }
}
