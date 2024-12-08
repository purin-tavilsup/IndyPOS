using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Common.Extensions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.InvoicePayments;
using IndyPOS.Application.UseCases.InvoiceProducts;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public class GetInvoiceInfoQueryHandler : IQueryHandler<GetInvoiceInfoQuery, IInvoiceInfo>
{
	private readonly IInvoiceProductRepository _invoiceProductRepository;
	private readonly IInvoicePaymentRepository _invoicePaymentRepository;
	public GetInvoiceInfoQueryHandler(IInvoiceProductRepository invoiceProductRepository, IInvoicePaymentRepository invoicePaymentRepository)
	{
		_invoiceProductRepository = invoiceProductRepository;
		_invoicePaymentRepository = invoicePaymentRepository;
	}

	public Task<IInvoiceInfo> Handle(GetInvoiceInfoQuery query, CancellationToken cancellationToken)
	{
		var invoiceInfo = CreateInvoiceInfo(query.InvoiceId);

		return Task.FromResult(invoiceInfo);
	}

	private IInvoiceInfo CreateInvoiceInfo(int invoiceId)
	{
		var products = GetProducts(invoiceId);
		var payments = GetPayments(invoiceId);
		var invoiceTotal = CalculateInvoiceTotal(products);
		var paymentTotal = CalculatePaymentTotal(payments);
		var isRefundInvoice = invoiceTotal < 0;
		var changes = CalculateChanges(invoiceTotal, paymentTotal);

		return new InvoiceInfo
		{
			Id = invoiceId,
			Products = products.ToProducts().ToList(),
			Payments = payments.ToPayments().ToList(),
			InvoiceTotal = invoiceTotal,
			PaymentTotal = paymentTotal,
			Changes = changes,
			IsRefundInvoice = isRefundInvoice,
			HasPayLaterPayment = payments.HasPayLayerPayment()
		};
	}

	private static decimal CalculateChanges(decimal invoiceTotal, decimal paymentTotal)
	{
		if (invoiceTotal < 0)
			return 0m;

		var amount = paymentTotal - invoiceTotal;

		return amount >= 0 ? amount : 0m;
	}

	private static decimal CalculateInvoiceTotal(IEnumerable<InvoiceProductDto> products)
    {
		return products.Sum(p => !p.IsGroupProduct ? p.UnitPrice * p.Quantity : p.GroupPrice);
	}

	private static decimal CalculatePaymentTotal(IEnumerable<InvoicePaymentDto> payments)
	{
		return payments.Sum(p => p.Amount);
	}

	private IList<InvoiceProductDto> GetProducts(int invoiceId)
	{
		var results = _invoiceProductRepository.GetByInvoiceId(invoiceId);

		return results.Select(x => x.ToDto())
					  .ToList();
	}

	private IList<InvoicePaymentDto> GetPayments(int invoiceId)
	{
		var results = _invoicePaymentRepository.GetByInvoiceId(invoiceId);

		return results.Select(x => x.ToDto())
					  .ToList();
	}
}