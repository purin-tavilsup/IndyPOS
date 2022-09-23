using IndyPOS.Common.Enums;
using IndyPOS.Common.Extensions;
using IndyPOS.Common.Interfaces;
using IndyPOS.DataAccess.Interfaces;
using IndyPOS.Facade.Adapters;
using IndyPOS.Facade.Interfaces;
using IndyPOS.Facade.Models.Report;
using IndyPOS.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IndyPOS.Controllers
{
	public class ReportController : IReportController
	{
		private readonly IReportHelper _reportHelper;
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

		public ReportController(IReportHelper reportHelper,
								IInvoiceRepository invoicesRepository,
								IAccountsReceivableRepository accountsReceivableRepository,
								IConfiguration configuration)
        {
			_reportHelper = reportHelper;
            _invoicesRepository = invoicesRepository;
            _accountsReceivableRepository = accountsReceivableRepository;
			_configuration = configuration;
        }

		public async Task<SalesReport> GetSaleReportAsync()
		{
			return await _reportHelper.GetSalesReportAsync();
		}

		public async Task<PaymentsReport> GetPaymentsReportAsync()
		{
			return await _reportHelper.GetPaymentsReportAsync();
		}

		public ArReport GetArReport()
		{
			return _reportHelper.GetArReport();
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
	}
}
