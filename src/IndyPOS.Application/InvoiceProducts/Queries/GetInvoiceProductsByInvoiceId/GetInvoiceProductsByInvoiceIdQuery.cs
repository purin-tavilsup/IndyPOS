using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByInvoiceId;

public record GetInvoiceProductsByInvoiceIdQuery(int InvoiceId) : IQuery<IEnumerable<InvoiceProductDto>>;