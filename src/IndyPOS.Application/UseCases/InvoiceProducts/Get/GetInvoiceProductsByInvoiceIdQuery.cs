using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Get;

public record GetInvoiceProductsByInvoiceIdQuery(int InvoiceId) : IQuery<IEnumerable<InvoiceProductDto>>;