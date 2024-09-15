using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.InvoicePayments;
using IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByDateRange;
using IndyPOS.Application.InvoicePayments.Queries.GetInvoicePaymentsByInvoiceId;
using IndyPOS.Application.InvoiceProducts;
using IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByDate;
using IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByDateRange;
using IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByInvoiceId;
using IndyPOS.Application.Invoices;
using IndyPOS.Application.Invoices.Get;
using IndyPOS.Application.Invoices.Queries.GetInvoicesByDateRange;
using IndyPOS.Application.PayLaterPayments;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentByInvoiceId;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPayments;
using IndyPOS.Application.PayLaterPayments.Queries.GetPayLaterPaymentsByDateRange;
using MediatR;

namespace IndyPOS.Infrastructure.Services;

public class ReportService : IReportService
{
	private readonly IMediator _mediator;

	public ReportService(IMediator mediator)
	{
		_mediator = mediator;
	}

	public async Task<IEnumerable<InvoiceDto>> GetInvoicesByPeriodAsync(TimePeriod period)
	{
		var dateRange = period.ToDateRange();

		return await GetInvoicesByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
	}

	public async Task<SalesSummary> CreateSalesSummaryByPeriodAsync(TimePeriod period)
	{
		var dateRange = period.ToDateRange();

		return await CreateSalesSummaryByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
	}

	public async Task<PaymentsSummary> CreatePaymentsSummaryByPeriodAsync(TimePeriod period)
	{
		var dateRange = period.ToDateRange();

		return await CreatePaymentsSummaryByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
	}

	public async Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsByPeriodAsync(TimePeriod period)
	{
		var dateRange = period.ToDateRange();

		return await GetPayLaterPaymentsByDateRangeAsync(dateRange.StartDate, dateRange.EndDate);
	}

	public async Task<SalesSummary> CreateSalesSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		var products = await GetInvoiceProductsByDateRangeAsync(startDate, endDate);
		var payLaterPayments = await GetPayLaterPaymentsByDateRangeAsync(startDate, endDate);

		return CreateSalesSummary(products.ToList(), payLaterPayments.ToList());
	}

	public async Task<SalesReport> CreateSalesReportByInvoiceIdAsync(int invoiceId, bool hasPayLaterPayment)
	{
		var id = Guid.NewGuid();
		var created = DateTime.Now;
		var summary = await CreateSalesSummaryByInvoiceIdAsync(invoiceId, hasPayLaterPayment);

		return summary.ToReport(id, created, invoiceId);
	}

	public async Task<PaymentsReport> CreatePaymentsReportByInvoiceIdAsync(int invoiceId)
	{
		var id = Guid.NewGuid();
		var created = DateTime.Now;
		var summary = await CreatePaymentsSummaryByInvoiceIdAsync(invoiceId);
		
		return summary.ToReport(id, created, invoiceId);
	}
	
	private async Task<SalesSummary> CreateSalesSummaryByInvoiceIdAsync(int invoiceId, bool hasPayLaterPayment)
	{
		var products = await GetInvoiceProductsByInvoiceIdAsync(invoiceId);
		
		List<PayLaterPaymentDto> payLaterPayments;

		if (hasPayLaterPayment)
		{
			var payLaterPayment = await GetPayLaterPaymentByInvoiceId(invoiceId);
			payLaterPayments = payLaterPayment is not null ? [payLaterPayment] : [];
		}
		else
		{
			payLaterPayments = [];
		}

		return CreateSalesSummary(products.ToList(), payLaterPayments);
	}

	private async Task<PaymentsSummary> CreatePaymentsSummaryByInvoiceIdAsync(int invoiceId)
	{
		var payments = await GetPaymentsByInvoiceIdAsync(invoiceId);

		return CreatePaymentsSummary(payments);
	}

	private static SalesSummary CreateSalesSummary(IReadOnlyList<InvoiceProductDto> products, 
												   IReadOnlyList<PayLaterPaymentDto> payLaterPayments)
	{
		var generalProductsTotal = 0m;
		var hardwareProductsTotal = 0m;
		var payLaterTotalForGeneralProducts = 0m;
		var payLaterTotalForHardwareProducts = 0m;
		
		var payLaterInvoiceIds = GetInvoiceIdsWithPayLaterPayment(payLaterPayments);

		foreach (var product in products)
		{
			var invoiceId = product.InvoiceId;
			var productTotal = !product.IsGroupProduct ? product.UnitPrice * product.Quantity : product.GroupPrice;

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

		return new SalesSummary
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

	public async Task<PaymentsSummary> CreatePaymentsSummaryByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		var payments = await GetPaymentsByDateRangeAsync(startDate, endDate);

		return CreatePaymentsSummary(payments);
	}

	private static PaymentsSummary CreatePaymentsSummary(IEnumerable<InvoicePaymentDto> payments)
	{
		var payLaterTotal = 0m;
		var fiftyFiftyTotal = 0m;
		var m33WeLoveTotal = 0m;
		var moneyTransferTotal = 0m;
		var welfareCardTotal = 0m;
		var weWinTotal = 0m;
		
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

		return new PaymentsSummary
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

	private async Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _mediator.Send(new GetInvoicePaymentsByDateRangeQuery(startDate, endDate));
	}

	public async Task<IEnumerable<InvoicePaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId)
	{
		return await _mediator.Send(new GetInvoicePaymentsByInvoiceIdQuery(invoiceId));
	}

	private async Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsByDateRangeAsync(DateOnly startDate, DateOnly endDate)
	{
		return await _mediator.Send(new GetPayLaterPaymentsByDateRangeQuery(startDate, endDate));
	}

	private async Task<PayLaterPaymentDto?> GetPayLaterPaymentByInvoiceId(int invoiceId)
	{
		return await _mediator.Send(new GetPayLaterPaymentByInvoiceIdQuery(invoiceId));
	}

	public async Task<IEnumerable<PayLaterPaymentDto>> GetPayLaterPaymentsAsync()
	{
		return await _mediator.Send(new GetPayLaterPaymentsQuery());
	}

	public async Task<IInvoiceInfo> GetInvoiceInfoAsync(int invoiceId)
	{
		return await _mediator.Send(new GetInvoiceInfoQuery(invoiceId));
	}
}