using Nokpirab;

namespace IndyPOS.Application.UseCases.Invoices.Get;

public record GetInvoiceByIdQuery(int Id) : IQuery<InvoiceDto>;