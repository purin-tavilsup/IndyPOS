using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoicePayments.Get;

public record GetInvoicePaymentsByDateQuery(DateOnly Date) : IQuery<IEnumerable<InvoicePaymentDto>>;