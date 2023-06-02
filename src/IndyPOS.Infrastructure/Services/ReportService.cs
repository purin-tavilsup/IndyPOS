using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.InvoicePayments;
using IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByDateRange;
using IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByInvoiceId;
using IndyPOS.Application.InvoiceProducts;
using IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByDate;
using IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByDateRange;
using IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByInvoiceId;
using IndyPOS.Application.Invoices;
using IndyPOS.Application.Invoices.Queries.GetInvoicesByDateRange;
using IndyPOS.Application.PayLaterPayments;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentByInvoiceId;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentsByDateRange;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace IndyPOS.Infrastructure.Services;

public class ReportService : IReportService
{
	private readonly IMediator _mediator;
	private readonly string _reportsDirectory;
	private readonly IJsonService _jsonService;
	private readonly ILogger<ReportService> _logger;
	private readonly IDataFeedApiService _dataFeedApiService;
	private readonly IEventAggregator _eventAggregator;
	private readonly IReadOnlyDictionary<int, string> _productCategories;
	private readonly IReadOnlyDictionary<int, string> _paymentTypes;

	public ReportService(IConfiguration configuration,
						 IStoreConstants storeConstants,
						 IMediator mediator,
						 IDataFeedApiService dataFeedApiService,
						 IEventAggregator eventAggregator,
						 ILogger<ReportService> logger, 
						 IJsonService jsonService)
	{
		_logger = logger;
		_mediator = mediator;
		_reportsDirectory = GetReportDirectory(configuration);
		_dataFeedApiService = dataFeedApiService;
		_eventAggregator = eventAggregator;
		_jsonService = jsonService;
		_productCategories = storeConstants.ProductCategories;
		_paymentTypes = storeConstants.PaymentTypes;
	}

	private static string GetReportDirectory(IConfiguration configuration)
	{
		var path = configuration.GetValue<string>("Report:Directory");

		return path ?? "C:\\ProgramData\\IndyPOS\\Reports";
	}

	public async Task<IEnumerable<InvoiceDto>> GetInvoicesByPeriodAsync(TimePeriod period)
	{
		var dateRange = period.ToDateRange();

		return await GetInvoicesByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
	}

	public async Task<ISalesReport> CreateSalesReportByPeriodAsync(TimePeriod period)
	{
		var dateRange = period.ToDateRange();

		return await CreateSalesReportByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
	}

	public async Task<IPaymentsReport> CreatePaymentsReportByPeriodAsync(TimePeriod period)
	{
		var dateRange = period.ToDateRange();

		return await CreatePaymentsReportByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
	}

	private async Task<ISalesReport> CreateSalesReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		var generalProductsTotal = 0m;
		var hardwareProductsTotal = 0m;
		var payLaterTotalForGeneralProducts = 0m;
		var payLaterTotalForHardwareProducts = 0m;
		
		var products = await GetInvoiceProductsByDateRangeAsync(startDate, endDate);
		var payLaterPayments = await GetPayLaterPaymentsByDateRangeAsync(startDate, endDate);

		var payLaterInvoiceIds = GetInvoiceIdsWithPayLaterPayment(payLaterPayments);

		foreach (var product in products)
		{
			var invoiceId = product.InvoiceId;
			var productTotal = product.UnitPrice * product.Quantity;

			if (IsGeneralProduct(product))
			{
				generalProductsTotal += productTotal;

				if (payLaterInvoiceIds.Contains(invoiceId))
				{
					payLaterTotalForGeneralProducts += productTotal;
				}
			}
			else
			{
				hardwareProductsTotal += productTotal;

				if (payLaterInvoiceIds.Contains(invoiceId))
				{
					payLaterTotalForHardwareProducts += productTotal;
				}
			}
		}

		var invoicesTotal = generalProductsTotal + hardwareProductsTotal;
		var payLaterPaymentsTotal = payLaterTotalForGeneralProducts + payLaterTotalForHardwareProducts;
		var invoiceTotalWithoutPayLaterPayments = invoicesTotal - payLaterPaymentsTotal;
		var generalProductsTotalWithoutPayLaterPayments = generalProductsTotal   - payLaterTotalForGeneralProducts;
		var hardwareProductsTotalWithoutPayLaterPayments = hardwareProductsTotal - payLaterTotalForHardwareProducts;

		var completedPayLaterPaymentsTotal = CalculateCompletedPayLaterPaymentsTotal(payLaterPayments);
		var incompletedPayLaterPaymentsTotal = payLaterPaymentsTotal - completedPayLaterPaymentsTotal;

		return new SalesReport
		{
			InvoiceTotal = invoicesTotal,
			GeneralProductsTotal = generalProductsTotal,
			HardwareProductsTotal = hardwareProductsTotal,
			PayLaterPaymentsTotal = payLaterPaymentsTotal,
			PayLaterPaymentsTotalForGeneralProducts = payLaterTotalForGeneralProducts,
			PayLaterPaymentsTotalForHardwareProducts = payLaterTotalForHardwareProducts,
			InvoiceTotalWithoutPayLaterPayments = invoiceTotalWithoutPayLaterPayments,
			GeneralProductsTotalWithoutPayLaterPayments = generalProductsTotalWithoutPayLaterPayments,
			HardwareProductsTotalWithoutPayLaterPayments = hardwareProductsTotalWithoutPayLaterPayments,
			CompletedPayLaterPaymentsTotal = completedPayLaterPaymentsTotal,
			IncompletePayLaterPaymentsTotal = incompletedPayLaterPaymentsTotal
		};
	}

	private static HashSet<int> GetInvoiceIdsWithPayLaterPayment(IEnumerable<PayLaterPaymentDto> payments)
	{
		return payments.Select(x => x.InvoiceId)
					   .ToHashSet();
	}

	private static decimal CalculateCompletedPayLaterPaymentsTotal(IEnumerable<PayLaterPaymentDto> payments)
	{
		return payments.Where(p => p.IsCompleted)
					   .Sum(x => x.PaidAmount);
	}

	private async Task<IPaymentsReport> CreatePaymentsReportByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		var payLaterTotal = 0m;
		var fiftyFiftyTotal = 0m;
		var m33WeLoveTotal = 0m;
		var moneyTransferTotal = 0m;
		var welfareCardTotal = 0m;
		var weWinTotal = 0m;

		var payments = await GetPaymentsByDateRangeAsync(startDate, endDate);

		foreach (var payment in payments)
		{
			var amount = payment.Amount;

			switch (payment.PaymentTypeId)
			{
				case (int) PaymentType.PayLater:
					payLaterTotal += amount;
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

		return new PaymentsReport
		{
			MoneyTransferTotal = moneyTransferTotal,
			FiftyFiftyTotal = fiftyFiftyTotal,
			M33WeLoveTotal = m33WeLoveTotal,
			WeWinTotal = weWinTotal,
			WelfareCardTotal = welfareCardTotal,
			PayLaterTotal = payLaterTotal
		};
	}

	private static bool IsGeneralProduct(InvoiceProductDto product)
	{
		return product.Category < (int) ProductCategory.Hardware;
	}

	public async Task<IEnumerable<InvoiceDto>> GetInvoicesByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _mediator.Send(new GetInvoicesByDateRangeQuery(startDate, endDate));
    }

	public async Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateAsync(DateOnly date)
	{
		return await _mediator.Send(new GetInvoiceProductsByDateQuery(date));
	}

	public async Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _mediator.Send(new GetInvoiceProductsByDateRangeQuery(startDate, endDate));
	}

	public async Task<IEnumerable<InvoiceProductDto>> GetInvoiceProductsByInvoiceIdAsync(int invoiceId)
	{
		return await _mediator.Send(new GetInvoiceProductsByInvoiceIdQuery(invoiceId));
	}

	public async Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _mediator.Send(new GetInvoicePaymentsByDateRangeQuery(startDate, endDate));
	}

	public async Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId)
	{
		return await _mediator.Send(new GetInvoicePaymentsByInvoiceIdQuery(invoiceId));
	}

	public async Task<IReadOnlyList<PayLaterPaymentDto>> GetPayLaterPaymentsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		var results = await _mediator.Send(new GetPayLaterPaymentsByDateRangeQuery(startDate, endDate));

		return results.ToList();
	}

	public async Task<PayLaterPaymentDto> GetPayLaterPaymentsByInvoiceIdAsync(int invoiceId)
	{
		return await _mediator.Send(new GetPayLaterPaymentByInvoiceIdQuery(invoiceId));
	}

	private class SalesReport : ISalesReport
	{
		public decimal InvoiceTotal { get; init; }
		public decimal GeneralProductsTotal { get; init; }
		public decimal HardwareProductsTotal { get; init; }
		public decimal PayLaterPaymentsTotal { get; init; }
		public decimal PayLaterPaymentsTotalForGeneralProducts { get; init; }
		public decimal PayLaterPaymentsTotalForHardwareProducts { get; init; }
		public decimal InvoiceTotalWithoutPayLaterPayments { get; init; }
		public decimal GeneralProductsTotalWithoutPayLaterPayments { get; init; }
		public decimal HardwareProductsTotalWithoutPayLaterPayments { get; init; }
		public decimal CompletedPayLaterPaymentsTotal { get; init; }
		public decimal IncompletePayLaterPaymentsTotal { get; init; }
	}

	private class PaymentsReport : IPaymentsReport
	{
		public decimal MoneyTransferTotal { get; set; }
		public decimal FiftyFiftyTotal { get; set; }
		public decimal M33WeLoveTotal { get; set; }
		public decimal WeWinTotal { get; set; }
		public decimal WelfareCardTotal { get; set; }
		public decimal PayLaterTotal { get; set; }
	}
}