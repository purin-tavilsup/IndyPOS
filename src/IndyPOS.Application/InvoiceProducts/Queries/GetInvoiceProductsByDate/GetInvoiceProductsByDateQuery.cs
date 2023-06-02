using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByDate;

public record GetInvoiceProductsByDateQuery(DateOnly Date) : IQuery<IEnumerable<InvoiceProductDto>>;