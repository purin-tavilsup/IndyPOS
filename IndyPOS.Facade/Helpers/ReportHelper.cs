﻿using IndyPOS.Common.Enums;
using IndyPOS.Common.Extensions;
using IndyPOS.Common.Interfaces;
using IndyPOS.Facade.Events;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using Prism.Events;
using Serilog;

namespace IndyPOS.Facade.Helpers
{
	public class ReportHelper : IReportHelper
	{
		private readonly string _reportsDirectory;
		private readonly IJsonUtility _jsonUtility;
		private readonly ILogger _logger; 
		private readonly ISaleInvoiceHelper _saleInvoiceHelper;
		private readonly IAccountsReceivableHelper _accountsReceivableHelper;
		private readonly IDataFeedApiHelper _dataFeedApiHelper;
		private readonly IEventAggregator _eventAggregator;
		private readonly IReadOnlyDictionary<int, string> _productCategories;
		private readonly IReadOnlyDictionary<int, string> _paymentTypes;

		public ReportHelper(IConfig config,
							IStoreConstants storeConstants,
							ISaleInvoiceHelper saleInvoiceHelper,
							IAccountsReceivableHelper accountsReceivableHelper,
							IDataFeedApiHelper dataFeedApiHelper,
							IEventAggregator eventAggregator,
							ILogger logger, 
							IJsonUtility jsonUtility)
        {
			_logger = logger;
			_reportsDirectory = config.ReportsDirectory;
			_saleInvoiceHelper = saleInvoiceHelper;
			_accountsReceivableHelper = accountsReceivableHelper;
			_dataFeedApiHelper = dataFeedApiHelper;
			_eventAggregator = eventAggregator;
			_jsonUtility = jsonUtility;
			_productCategories = storeConstants.ProductCategories;
			_paymentTypes = storeConstants.PaymentTypes;
        }

		public async Task<SalesReport> GetSalesReportAsync()
		{
			var today = DateTime.Now;
			var filePath = $"{_reportsDirectory}\\SalesReport-{today.Year}.json";

			return await GetSalesReportFromFile(filePath, today);
		}

		public async Task<PaymentsReport> GetPaymentsReportAsync()
		{
			var today = DateTime.Now;
			var filePath = $"{_reportsDirectory}\\PaymentsReport-{today.Year}.json";

			return await GetPaymentsReportFromFile(filePath, today);
		}

		public ArReport GetArReport()
		{
			var today = DateTime.Now;
			var report = new ArReport
			{
				Id = $"{today.Year}",
				YearSummary = GetYearArSummary(),
				MonthSummary = GetMonthArSummary(),
				DaySummary = GetDayArSummary(),
				LastUpdateDateTime = today
			};

			return report;
		}

		private async Task<SalesReport> UpdateSalesReport(SalesSummary summary)
		{
			var today = DateTime.Now;
			var filePath = $"{_reportsDirectory}\\SalesReport-{today.Year}.json";
			var report = await GetSalesReportFromFile(filePath, today);

			report.YearSummary = GetYearSalesSummary(report, summary, today);
			report.MonthSummary = GetMonthSalesSummary(report, summary, today);
			report.DaySummary = GetDaySalesSummary(report, summary, today);
			report.LastUpdateDateTime = today;

			await SaveReportToFile(report, filePath);

			return report;
		}

		private async Task UpdatePaymentsReport(PaymentsSummary summary)
		{
			var today = DateTime.Now;
			var filePath = $"{_reportsDirectory}\\PaymentsReport-{today.Year}.json";
			var report = await GetPaymentsReportFromFile(filePath, today);

			report.YearSummary = GetYearPaymentsSummary(report, summary, today);
			report.MonthSummary = GetMonthPaymentsSummary(report, summary, today);
			report.DaySummary = GetDayPaymentsSummary(report, summary, today);
			report.LastUpdateDateTime = today;

			await SaveReportToFile(report, filePath);
		}

		private static SalesSummary GetYearSalesSummary(SalesReport report, SalesSummary summary, DateTime today)
		{
			var resetRequired = report.LastUpdateDateTime.Year != today.Year;

			return resetRequired ? summary : UpdateSalesSummary(report.YearSummary, summary);
		}

		private static SalesSummary GetMonthSalesSummary(SalesReport report, SalesSummary summary, DateTime today)
		{
			var resetRequired = report.LastUpdateDateTime.ToString("MM-yyyy") != today.ToString("MM-yyyy");

			return resetRequired ? summary : UpdateSalesSummary(report.MonthSummary, summary);
		}

		private static SalesSummary GetDaySalesSummary(SalesReport report, SalesSummary summary, DateTime today)
        {
			var resetRequired = report.LastUpdateDateTime.ToString("MM-dd-yyyy") != today.ToString("MM-dd-yyyy");

			return resetRequired ? summary : UpdateSalesSummary(report.DaySummary, summary);
		}

		private static PaymentsSummary GetYearPaymentsSummary(PaymentsReport report, PaymentsSummary summary, DateTime today)
		{
			var resetRequired = report.LastUpdateDateTime.Year != today.Year;

			return resetRequired ? summary : UpdatePaymentsSummary(report.YearSummary, summary);
		}

		private static PaymentsSummary GetMonthPaymentsSummary(PaymentsReport report, PaymentsSummary summary, DateTime today)
		{
			var resetRequired = report.LastUpdateDateTime.ToString("MM-yyyy") != today.ToString("MM-yyyy");

			return resetRequired ? summary : UpdatePaymentsSummary(report.MonthSummary, summary);
		}

		private static PaymentsSummary GetDayPaymentsSummary(PaymentsReport report, PaymentsSummary summary, DateTime today)
		{
			var resetRequired = report.LastUpdateDateTime.ToString("MM-dd-yyyy") != today.ToString("MM-dd-yyyy");

			return resetRequired ? summary : UpdatePaymentsSummary(report.DaySummary, summary);
		}

		private static SalesSummary UpdateSalesSummary(SalesSummary previousSummary, SalesSummary newSummary)
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

		private static PaymentsSummary UpdatePaymentsSummary(PaymentsSummary previousSummary, PaymentsSummary newSummary)
		{
			return new PaymentsSummary
			{
				MoneyTransferTotal = previousSummary.MoneyTransferTotal + newSummary.MoneyTransferTotal,
				FiftyFiftyTotal = previousSummary.FiftyFiftyTotal + newSummary.FiftyFiftyTotal,
				M33WeLoveTotal = previousSummary.M33WeLoveTotal + newSummary.M33WeLoveTotal,
				WeWinTotal = previousSummary.WeWinTotal + newSummary.WeWinTotal,
				WelfareCardTotal = previousSummary.WelfareCardTotal + newSummary.WelfareCardTotal,
				ArTotal = previousSummary.ArTotal + newSummary.ArTotal
			};
		}

		private async Task SaveReportToFile<TValue>(TValue report, string filePath)
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

		private async Task<SalesReport> GetSalesReportFromFile(string filePath, DateTime today)
		{
			try
			{
				if (!File.Exists(filePath))
                {
					var report = CreateNewSalesReport(today);

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

		private async Task<PaymentsReport> GetPaymentsReportFromFile(string filePath, DateTime today)
		{
			try
			{
				if (!File.Exists(filePath))
				{
					var report = CreateNewPaymentsReport(today);

					await SaveReportToFile(report, filePath);
				}

				return await _jsonUtility.ReadFromFileAsync<PaymentsReport>(filePath);
			}
			catch (Exception ex)
			{
				_logger.Error(ex, "Failed to get report from file.");
				throw;
			}
		}

		private static SalesReport CreateNewSalesReport(DateTime today)
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

		private static PaymentsReport CreateNewPaymentsReport(DateTime today)
		{
			return new PaymentsReport
			{
				Id = $"{today.Year}",
				DaySummary = new PaymentsSummary(),
				MonthSummary = new PaymentsSummary(),
				YearSummary = new PaymentsSummary(),
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

		public PaymentsSummary CreatePaymentsSummary(IInvoiceInfo invoiceInfo)
		{
			var arTotal = 0m;
			var fiftyFiftyTotal = 0m;
			var m33WeLoveTotal = 0m;
			var moneyTransferTotal = 0m;
			var welfareCardTotal = 0m;
			var weWinTotal = 0m;
			var payments = invoiceInfo.Payments;

			foreach (var payment in payments)
			{
				var amount = payment.Amount;

				switch (payment.PaymentTypeId)
				{
					case (int) PaymentType.AccountReceivable:
						arTotal += amount;
						break;

					case (int) PaymentType.WelfareCard:
						welfareCardTotal += amount;
						break;

					case (int) PaymentType.M33WeLove:
						m33WeLoveTotal += amount;
						break;

					case (int) PaymentType.MoneyTransfer:
						moneyTransferTotal += amount;
						break;

					case (int) PaymentType.FiftyFifty:
						fiftyFiftyTotal += amount;
						break;

					case (int) PaymentType.WeWin:
						weWinTotal += amount;
						break;
				}
			}

			var summary = new PaymentsSummary
			{
				MoneyTransferTotal = (double) moneyTransferTotal,
				FiftyFiftyTotal = (double) fiftyFiftyTotal,
				M33WeLoveTotal = (double) m33WeLoveTotal,
				WeWinTotal = (double) weWinTotal,
				WelfareCardTotal = (double) welfareCardTotal,
				ArTotal = (double) arTotal,
			};

			return summary;
		}

		private static bool HasArPayment(IInvoiceInfo invoiceInfo)
		{
			return invoiceInfo.Payments.Any(p => p.PaymentTypeId == (int) PaymentType.AccountReceivable);
		}

		private Invoice CreateInvoiceForDataFeed(IInvoiceInfo invoiceInfo)
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

		private ArSummary GetYearArSummary()
		{
			var startDate = DateTime.Today.FirstDayOfYear();
			var endDate = DateTime.Today.LastDayOfYear();

			var ar = _accountsReceivableHelper.GetAccountsReceivablesByDateRange(startDate, endDate);

			return CreateArSummary(ar);
		}

		private ArSummary GetMonthArSummary()
		{
			var startDate = DateTime.Today.FirstDayOfMonth();
			var endDate = DateTime.Today.LastDayOfMonth();

			var ar = _accountsReceivableHelper.GetAccountsReceivablesByDateRange(startDate, endDate);

			return CreateArSummary(ar);
		}

		private ArSummary GetDayArSummary()
		{
			var startDate = DateTime.Today;
			var endDate = DateTime.Today;

			var ar = _accountsReceivableHelper.GetAccountsReceivablesByDateRange(startDate, endDate);

			return CreateArSummary(ar);
		}

		private static ArSummary CreateArSummary(IEnumerable<IAccountsReceivable> accountsReceivables)
		{
			var arTotal = 0m;
			var completedArTotal = 0m;

			foreach (var ar in accountsReceivables)
			{
				var amount = ar.ReceivableAmount;

				arTotal += amount;

				if (ar.IsCompleted)
					completedArTotal += amount;
			}

			var incompleteArTotal = arTotal - completedArTotal;

			var summary = new ArSummary
			{
				ArTotal = (double) arTotal,
				CompletedArTotal = (double) completedArTotal,
				IncompleteArTotal = (double) incompleteArTotal
			};

			return summary;
		}

		public IEnumerable<IFinalInvoice> GetInvoicesByPeriod(TimePeriod period)
		{
			return _saleInvoiceHelper.GetInvoicesByPeriod(period);
		}

		public IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateTime startDate, DateTime endDate)
		{
			return _saleInvoiceHelper.GetInvoicesByDateRange(startDate, endDate);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date)
		{
			return _saleInvoiceHelper.GetInvoiceProductsByDate(date);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateTime startDate, DateTime endDate)
		{
			return _saleInvoiceHelper.GetInvoiceProductsByDateRange(startDate, endDate);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId)
		{
			return _saleInvoiceHelper.GetInvoiceProductsByInvoiceId(invoiceId);
		}

		public IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId)
		{
			return _saleInvoiceHelper.GetPaymentsByInvoiceId(invoiceId);
		}

		public async Task UpdateReportAsync(IInvoiceInfo invoiceInfo)
		{
			var salesSummary = CreateSalesSummary(invoiceInfo);
			var paymentsSummary = CreatePaymentsSummary(invoiceInfo);
			
			var salesReportToPush = await UpdateSalesReport(salesSummary);
			await UpdatePaymentsReport(paymentsSummary);

			var invoiceToPush = CreateInvoiceForDataFeed(invoiceInfo);
			await _dataFeedApiHelper.PushInvoice(invoiceToPush);
			await _dataFeedApiHelper.PushReport(salesReportToPush);

			var dataFeedStatus = "DataFeed Push : " + salesReportToPush.LastUpdateDateTime.ToString("O");

			_eventAggregator.GetEvent<SalesReportPushedEvent>().Publish(dataFeedStatus);
		}
    }
}
