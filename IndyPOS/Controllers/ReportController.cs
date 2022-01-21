using CsvHelper;
using IndyPOS.Adapters;
using IndyPOS.DataAccess.Repositories;
using IndyPOS.Enums;
using IndyPOS.Extensions;
using IndyPOS.Sales;
using IndyPOS.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IndyPOS.Controllers
{
    internal class ReportController : IReportController
	{
		private readonly IUserAccountHelper _accountHelper;
		private readonly IInvoiceRepository _invoicesRepository;
		private readonly IAccountsReceivableRepository _accountsReceivableRepository;
		private readonly IInventoryProductRepository _inventoryProductsRepository;
		private readonly IConfig _config;

		private IEnumerable<IFinalInvoice> _invoices;
		private IEnumerable<IFinalInvoiceProduct> _invoiceProducts;
		private IEnumerable<IFinalInvoicePayment> _payments;
		private IEnumerable<IAccountsReceivable> _accountsReceivables;

		public IEnumerable<IFinalInvoice> Invoices => _invoices ?? new List<IFinalInvoice>();

		public IEnumerable<IFinalInvoiceProduct> InvoiceProducts => _invoiceProducts ?? new List<IFinalInvoiceProduct>();

		public IEnumerable<IFinalInvoicePayment> InvoicePayments => _payments ?? new List<IFinalInvoicePayment>();

		public IEnumerable<IAccountsReceivable> AccountsReceivables => _accountsReceivables ?? new List<IAccountsReceivable>();

		public IEnumerable<IFinalInvoiceProduct> GeneralGoodsProducts => GetGeneralGoodsProducts();

		public IEnumerable<IFinalInvoiceProduct> HardwareProducts => GetHardwareProducts();

		public decimal InvoicesTotal => GetInvoicesTotal();

		public decimal PaymentsTotal => GetPaymentsTotal();

		public decimal ChangesTotal => GetChangesTotal();

		public decimal RefundTotal => GetRefundTotal();

		public decimal ArTotal => GetArTotal();

		public decimal CompletedArTotal => GetCompletedArTotal();

		public decimal IncompleteArTotal => GetIncompleteArTotal();

		public decimal InvoicesTotalWithoutAr => GetInvoicesTotalWithoutAr();

		public decimal InvoicesTotalWithoutIncompleteAr => GetInvoicesTotalWithoutIncompleteAr();

		public decimal GeneralGoodsProductsTotal => GetGeneralGoodsProductsTotal();

		public decimal HardwareProductsTotal => GetHardwareProductsTotal();

        public ReportController(IUserAccountHelper accountHelper,
								IInvoiceRepository invoicesRepository,
								IAccountsReceivableRepository accountsReceivableRepository,
								IInventoryProductRepository inventoryProductsRepository,
								IConfig config)
		{
			_accountHelper = accountHelper;
			_invoicesRepository = invoicesRepository;
			_accountsReceivableRepository = accountsReceivableRepository;
			_inventoryProductsRepository = inventoryProductsRepository;
			_config = config;
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

			_invoices = GetInvoicesByDateRange(startDate, endDate);
			_invoiceProducts = GetInvoiceProductsByDateRange(startDate, endDate);
			_payments = GetPaymentsByDateRange(startDate, endDate);
			_accountsReceivables = GetAccountsReceivablesByDateRange(startDate, endDate);
		}

		public void LoadInvoicesByDateRange(DateTime startDate, DateTime endDate)
		{
			_invoices = GetInvoicesByDateRange(startDate, endDate);
			_invoiceProducts = GetInvoiceProductsByDateRange(startDate, endDate);
			_payments = GetPaymentsByDateRange(startDate, endDate);
			_accountsReceivables = GetAccountsReceivablesByDateRange(startDate, endDate);
		}

		private decimal GetInvoicesTotal()
		{
			return Invoices.Sum(x => x.Total);
		}

		private decimal GetPaymentsTotal()
		{
			return InvoicePayments.Sum(x => x.Amount);
		}

		private decimal GetChangesTotal()
		{
			var changes = 0m;

			foreach (var invoice in _invoices)
			{
				if (invoice.Total <= 0m)
					continue;

				var payment = _payments.Where(x => x.InvoiceId == invoice.InvoiceId)
									   .Sum(x => x.Amount);

				changes += payment - invoice.Total;
			}

			return changes;
		}

		private decimal GetRefundTotal()
		{
			return _invoices.Where(x => x.Total < 0)
							.Sum(x => x.Total);
		}

		private decimal GetInvoicesTotalWithoutAr()
		{
			return InvoicesTotal - ArTotal;
		}

		private decimal GetInvoicesTotalWithoutIncompleteAr()
        {
			return InvoicesTotal - IncompleteArTotal;
        }

		private decimal GetArTotal()
		{
			return _accountsReceivables.Sum(x => x.ReceivableAmount);
		}

		private decimal GetCompletedArTotal()
        {
			return _accountsReceivables.Where(x => x.IsCompleted)
									   .Sum(x => x.ReceivableAmount);
        }

		private decimal GetIncompleteArTotal()
		{
			return _accountsReceivables.Where(x => !x.IsCompleted)
									   .Sum(x => x.ReceivableAmount);
		}

		private decimal GetGeneralGoodsProductsTotal()
        {
			return GeneralGoodsProducts.Sum(x => (x.UnitPrice * x.Quantity));
        }

		private decimal GetHardwareProductsTotal()
		{
			return HardwareProducts.Sum(x => (x.UnitPrice * x.Quantity));
		}

		private IEnumerable<IFinalInvoiceProduct> GetGeneralGoodsProducts()
		{
			return InvoiceProducts.Where(p => p.Category < (int) ProductCategory.Hardware);
		}

		private IEnumerable<IFinalInvoiceProduct> GetHardwareProducts()
		{
			return InvoiceProducts.Where(p => p.Category >= (int) ProductCategory.Hardware);
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

		public decimal GetPaymentsTotalByType(PaymentType type)
		{
			var paymentsByType = _payments.Where(x => x.PaymentTypeId == (int) type);

			return paymentsByType.Sum(x => x.Amount);
		}

		public void WriteSaleRecordsToCsvFileByDate(DateTime date)
		{
			var directoryPath = $"{_config.ReportDirectory}\\{date.Year}\\{date.Month:00}\\{date:yyyy-MMM-dd}";
			
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
