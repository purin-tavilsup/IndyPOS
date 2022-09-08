using IndyPOS.Common.Interfaces;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IndyPOS.Common.Enums;

namespace IndyPOS.Facade.Helpers
{
	public class ReportHelper : IReportHelper
	{
		private readonly string _reportsDirectory;
		private readonly IJsonUtility _jsonUtility;
		private readonly ILogger _logger; 
		private readonly IInvoiceRepository _invoicesRepository;
		private readonly IAccountsReceivableRepository _accountsReceivableRepository;
		private readonly IConfiguration _configuration;

		private IEnumerable<IFinalInvoice> _invoices;
		private IEnumerable<IFinalInvoiceProduct> _invoiceProducts;
		private IEnumerable<IFinalInvoicePayment> _payments;
		private IEnumerable<IAccountsReceivable> _accountsReceivables;

		private readonly IReadOnlyDictionary<int, string> _productCategories;
		private readonly IReadOnlyDictionary<int, string> _paymentTypes;

		public IEnumerable<IFinalInvoice> Invoices => _invoices ?? new List<IFinalInvoice>();

		public IEnumerable<IFinalInvoiceProduct> InvoiceProducts => _invoiceProducts ?? new List<IFinalInvoiceProduct>();

		public IEnumerable<IFinalInvoicePayment> InvoicePayments => _payments ?? new List<IFinalInvoicePayment>();
		public IEnumerable<IAccountsReceivable> AccountsReceivables => _accountsReceivables ?? new List<IAccountsReceivable>();

		public ReportHelper(IConfiguration configuration,
							IStoreConstants storeConstants,
							ILogger logger, 
							IJsonUtility jsonUtility)
        {
			_logger = logger;
			_reportsDirectory = configuration.ReportsDirectory;
			_jsonUtility = jsonUtility;
			_productCategories = storeConstants.ProductCategories;
			_paymentTypes = storeConstants.PaymentTypes;
        }

		public async Task<SalesReport> UpdateReport(SalesSummary summary)
		{
			var today = DateTime.Now;
			var filePath = $"{_reportsDirectory}\\SalesReport-{today.Year}.json";
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

		public SalesSummary CreateSalesSummary(IInvoiceInfo invoiceInfo)
		{
			var generalProductsTotal = 0m;
			var hardwareProductsTotal = 0m;
			var invoiceProducts = invoiceInfo.Products;

			foreach (var product in invoiceProducts)
			{
				var productTotal = product.UnitPrice * product.Quantity;

				if (product.Category < (int) ProductCategory.Hardware)
				{
					generalProductsTotal += productTotal;
				}
				else
				{
					hardwareProductsTotal += productTotal;
				}
			}

			var hasArPayment = HasArPayment(invoiceInfo);
			var arTotalForGeneralProducts = hasArPayment ? generalProductsTotal : 0m;
			var arTotalForHardwareProducts = hasArPayment ? hardwareProductsTotal : 0m;
			var arTotal = hasArPayment ? arTotalForGeneralProducts + arTotalForHardwareProducts : 0m;
			var invoiceTotalWithoutAr = hasArPayment ? 0m : invoiceInfo.InvoiceTotal;
			var generalProductsTotalWithoutAr = hasArPayment ? 0m : generalProductsTotal;
			var hardwareProductsTotalWithoutAr = hasArPayment ? 0m : hardwareProductsTotal;

			var summary = new SalesSummary
			{
				InvoiceTotal = (double) invoiceInfo.InvoiceTotal,
				GeneralProductsTotal = (double) generalProductsTotal,
				HardwareProductsTotal = (double) hardwareProductsTotal,
				ArTotal = (double) arTotal,
				ArTotalForGeneralProducts = (double) arTotalForGeneralProducts,
				ArTotalForHardwareProducts = (double) arTotalForHardwareProducts,
				InvoiceTotalWithoutAr = (double) invoiceTotalWithoutAr,
				GeneralProductsTotalWithoutAr = (double) generalProductsTotalWithoutAr,
				HardwareProductsTotalWithoutAr = (double) hardwareProductsTotalWithoutAr
			};

			return summary;
		}

		private static bool HasArPayment(IInvoiceInfo invoiceInfo)
		{
			return invoiceInfo.Payments.Any(p => p.PaymentTypeId == (int) PaymentType.AccountReceivable);
		}

		public Invoice CreateInvoiceForDataFeed(IInvoiceInfo invoiceInfo)
		{
			return new Invoice
			{
				Id = invoiceInfo.Id.ToString(),
				Date = DateTime.Now.ToString("yyyy-M-d"),
				DateTime = DateTime.UtcNow,
				Products = MapProducts(invoiceInfo.Products),
				Payments = MapPayments(invoiceInfo.Payments)
			};
		}

		private Product[] MapProducts(IEnumerable<ISaleInvoiceProduct> invoiceProducts)
		{
			return invoiceProducts.Select(MapProduct)
								  .ToArray();
		}

		private Payment[] MapPayments(IEnumerable<IPayment> invoicePayments)
		{
			return invoicePayments.Select(MapPayment)
								  .ToArray();
		}

		private Product MapProduct(ISaleInvoiceProduct invoiceProduct)
		{
			return new Product
			{
				Priority = invoiceProduct.Priority,
				Description = invoiceProduct.Description,
				Barcode = invoiceProduct.Barcode,
				Category = _productCategories[invoiceProduct.Category],
				Group = invoiceProduct.Category < (int) ProductCategory.Hardware ? "General" : "Hardware",
				UnitPrice = (double) invoiceProduct.UnitPrice,
				Quantity = invoiceProduct.Quantity,
				Note = invoiceProduct.Note,
				DateTime = DateTime.UtcNow
			};
		}

		private Payment MapPayment(IPayment invoicePayment)
		{
			return new Payment
			{
				Priority = invoicePayment.Priority,
				PaymentType = _paymentTypes[invoicePayment.PaymentTypeId],
				Amount = (double) invoicePayment.Amount,
				Note = invoicePayment.Note,
				DateTime = DateTime.UtcNow
			};
		}
    }
}
