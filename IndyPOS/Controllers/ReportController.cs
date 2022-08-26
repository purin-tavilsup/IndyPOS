using CsvHelper;
using IndyPOS.Adapters;
using IndyPOS.Common.Extensions;
using IndyPOS.Common.Interfaces;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Enums;
using IndyPOS.Interfaces;
using IndyPOS.Sales;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SalesReport = IndyPOS.Sales.SalesReport;

namespace IndyPOS.Controllers
{
	public class ReportController : IReportController
	{
		private readonly IUserAccountHelper _accountHelper;
		private readonly IInvoiceRepository _invoicesRepository;
		private readonly IAccountsReceivableRepository _accountsReceivableRepository;
		private readonly IConfiguration _configuration;

		private IEnumerable<IFinalInvoice> _invoices;
		private IEnumerable<IFinalInvoiceProduct> _invoiceProducts;
		private IEnumerable<IFinalInvoicePayment> _payments;
		private IEnumerable<IAccountsReceivable> _accountsReceivables;

		public IEnumerable<IFinalInvoice> Invoices => _invoices ?? new List<IFinalInvoice>();

		public IEnumerable<IFinalInvoiceProduct> InvoiceProducts => _invoiceProducts ?? new List<IFinalInvoiceProduct>();

		public IEnumerable<IFinalInvoicePayment> InvoicePayments => _payments ?? new List<IFinalInvoicePayment>();

		public IEnumerable<IAccountsReceivable> AccountsReceivables => _accountsReceivables ?? new List<IAccountsReceivable>();

		public ReportController(IUserAccountHelper accountHelper,
                                IInvoiceRepository invoicesRepository,
                                IAccountsReceivableRepository accountsReceivableRepository,
								IConfiguration configuration)
        {
            _accountHelper = accountHelper;
            _invoicesRepository = invoicesRepository;
            _accountsReceivableRepository = accountsReceivableRepository;
			_configuration = configuration;
        }

        public void LoadInvoicesByPeriod(ReportPeriod period)
		{
			DateTime startDate;
			DateTime endDate;

			switch (period)
			{
				case ReportPeriod.Today:
					startDate = DateTime.Today;
					endDate = DateTime.Today;
					break;

				case ReportPeriod.ThisWeek:
					startDate = DateTime.Today.FirstDayOfWeek();
					endDate = DateTime.Today.LastDayOfWeek();
					break;

				case ReportPeriod.ThisMonth:
					startDate = DateTime.Today.FirstDayOfMonth();
					endDate = DateTime.Today.LastDayOfMonth();
					break;

				case ReportPeriod.ThisYear:
					startDate = DateTime.Today.FirstDayOfYear();
					endDate = DateTime.Today.LastDayOfYear();
					break;

				default:
					startDate = DateTime.Today;
					endDate = DateTime.Today;
					break;
			}

			LoadInvoicesByDateRange(startDate, endDate);
		}

		public void LoadInvoicesByDateRange(DateTime startDate, DateTime endDate)
		{
			_invoices = GetInvoicesByDateRange(startDate, endDate);
			_invoiceProducts = GetInvoiceProductsByDateRange(startDate, endDate);
			_payments = GetPaymentsByDateRange(startDate, endDate);
			_accountsReceivables = GetAccountsReceivablesByDateRange(startDate, endDate);
		}

		public SalesReport GetSaleReport()
		{
			var generalProductsTotal = 0m;
			var hardwareProductsTotal = 0m;
			var arTotalForGeneralProducts = 0m;
			var arTotalForHardwareProducts = 0m;

			var arInvoiceIds = _payments.Where(IsArPayment)
										.Select(x => x.InvoiceId)
										.ToHashSet();

			foreach (var product in _invoiceProducts)
			{
				var invoiceId = product.InvoiceId;
				var productTotal = product.UnitPrice * product.Quantity;

				if (IsGeneralProduct(product))
				{
					generalProductsTotal += productTotal;

					if (arInvoiceIds.Contains(invoiceId))
						arTotalForGeneralProducts += productTotal;
                }
				else
				{
					hardwareProductsTotal += productTotal;

					if (arInvoiceIds.Contains(invoiceId))
						arTotalForHardwareProducts += productTotal;
				}
			}

			var report = CreateSaleReport(generalProductsTotal,
										  hardwareProductsTotal,
										  arTotalForGeneralProducts,
										  arTotalForHardwareProducts);

			return report;
		}

		private static SalesReport CreateSaleReport(decimal generalProductsTotal,
												   decimal hardwareProductsTotal,
												   decimal arTotalForGeneralProducts,
												   decimal arTotalForHardwareProducts)
        {
			var invoicesTotal = generalProductsTotal + hardwareProductsTotal;
			var arTotal = arTotalForGeneralProducts + arTotalForHardwareProducts;
			var invoicesTotalWithoutAr = invoicesTotal - arTotal;
			var generalProductsTotalWithoutAr = generalProductsTotal - arTotalForGeneralProducts;
			var hardwareProductsTotalWithoutAr = hardwareProductsTotal - arTotalForHardwareProducts;

			var report = new SalesReport
						 {
							 InvoicesTotal = invoicesTotal,
							 GeneralProductsTotal = generalProductsTotal,
							 HardwareProductsTotal = hardwareProductsTotal,
							 InvoicesTotalWithoutAr = invoicesTotalWithoutAr,
							 ArTotalForGeneralProducts = arTotalForGeneralProducts,
							 ArTotalForHardwareProducts = arTotalForHardwareProducts,
							 GeneralProductsTotalWithoutAr = generalProductsTotalWithoutAr,
							 HardwareProductsTotalWithoutAr = hardwareProductsTotalWithoutAr
						 };

			return report;
        }

		public PaymentReport GetPaymentReport()
		{
			var arTotal = 0m;
			var fiftyFiftyTotal = 0m;
			var m33WeLoveTotal = 0m;
			var moneyTransferTotal = 0m;
			var welfareCardTotal = 0m;
			var weWinTotal = 0m;

			foreach (var payment in _payments)
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

			var report = new PaymentReport
						 {
							 ArTotal = arTotal,
							 FiftyFiftyTotal = fiftyFiftyTotal,
							 M33WeLoveTotal = m33WeLoveTotal,
							 MoneyTransferTotal = moneyTransferTotal,
							 WelfareCardTotal = welfareCardTotal,
							 WeWinTotal = weWinTotal
						 };

			return report;
		}

		public ArReport GetArReport()
		{
			var arTotal = 0m;
			var completedArTotal = 0m;

			foreach (var ar in _accountsReceivables)
			{
				var amount = ar.ReceivableAmount;

				arTotal += amount;

				if (ar.IsCompleted)
					completedArTotal += amount;
			}

			var incompleteArTotal = arTotal - completedArTotal;
			var report = new ArReport
						 {
							 ArTotal = arTotal,
							 CompletedArTotal = completedArTotal,
							 IncompleteArTotal = incompleteArTotal
						 };

			return report;
		}

		private static bool IsArPayment(IFinalInvoicePayment payment)
        {
			return payment.PaymentTypeId == (int) PaymentType.AccountReceivable;
        }

		private static bool IsGeneralProduct(IFinalInvoiceProduct product)
		{
			return product.Category < (int) ProductCategory.Hardware;
		}
		
		private IEnumerable<IFinalInvoice> GetInvoicesByDateRange(DateTime startDate, DateTime endDate)
		{
			var results = _invoicesRepository.GetInvoicesByDateRange(startDate, endDate);

			return results.Select(x => new FinalInvoiceAdapter(x) as IFinalInvoice);
		}

		private IEnumerable<IAccountsReceivable> GetAccountsReceivablesByDateRange(DateTime startDate, DateTime endDate)
		{
			var results = _accountsReceivableRepository.GetAccountsReceivablesByDateRange(startDate, endDate);

			return results.Select(x => new AccountsReceivableAdapter(x) as IAccountsReceivable);
		}

		private IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDateRange(DateTime startDate, DateTime endDate)
		{
			var results = _invoicesRepository.GetInvoiceProductsByDateRange(startDate, endDate);

			return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByDate(DateTime date)
		{
			var results = _invoicesRepository.GetInvoiceProductsByDate(date);

			return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
		}

		private IEnumerable<IFinalInvoicePayment> GetPaymentsByDateRange(DateTime startDate, DateTime endDate)
		{
			var results = _invoicesRepository.GetPaymentsByDateRange(startDate, endDate);

			return results.Select(x => new FinalInvoicePaymentAdapter(x) as IFinalInvoicePayment);
		}

		public IFinalInvoice GetInvoiceByInvoiceId(int invoiceId)
		{
			var result = _invoicesRepository.GetInvoiceByInvoiceId(invoiceId);

			return result != null ? new FinalInvoiceAdapter(result) : null;
		}

		public IEnumerable<IFinalInvoiceProduct> GetInvoiceProductsByInvoiceId(int invoiceId)
		{
			var results = _invoicesRepository.GetInvoiceProductsByInvoiceId(invoiceId);

			return results.Select(x => new FinalInvoiceProductAdapter(x) as IFinalInvoiceProduct);
		}

		public IEnumerable<IFinalInvoicePayment> GetPaymentsByInvoiceId(int invoiceId)
		{
			var results = _invoicesRepository.GetPaymentsByInvoiceId(invoiceId);

			return results.Select(x => new FinalInvoicePaymentAdapter(x) as IFinalInvoicePayment);
		}

		public void WriteSaleRecordsToCsvFileByDate(DateTime date)
		{
			var directoryPath = $"{_configuration.ReportsDirectory}\\{date.Year}\\{date.Month:00}\\{date:yyyy-MMM-dd}";
			
			if (!Directory.Exists(directoryPath)) 
				Directory.CreateDirectory(directoryPath);

			WriteInvoiceRecordsToCsvFileByDate(directoryPath, date);
			WriteInvoiceProductRecordsToCsvFileByDate(directoryPath, date);
			WritePaymentRecordsToCsvFileByDate(directoryPath, date);
		}

		private void WriteInvoiceRecordsToCsvFileByDate(string directoryPath, DateTime date)
		{
			var filePath = $"{directoryPath}\\Invoices.csv";
			var invoices = _invoicesRepository.GetInvoicesByDate(date);

			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(invoices);
			}
        }

		private void WriteInvoiceProductRecordsToCsvFileByDate(string directoryPath, DateTime date)
        {
			var filePath = $"{directoryPath}\\InvoiceProducts.csv";
			var products = _invoicesRepository.GetInvoiceProductsByDate(date);

			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(products);
			}
        }

		private void WritePaymentRecordsToCsvFileByDate(string directoryPath, DateTime date)
        {
			var filePath = $"{directoryPath}\\Payments.csv";
			var payments = _invoicesRepository.GetPaymentsByDate(date);

			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(payments);
			}
        }
	}
}
