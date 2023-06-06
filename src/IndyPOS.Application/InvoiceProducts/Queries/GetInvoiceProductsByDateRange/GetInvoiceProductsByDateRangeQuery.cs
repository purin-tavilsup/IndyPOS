using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InvoiceProducts.Queries.GetInvoiceProductsByDateRange;

public record GetInvoiceProductsByDateRangeQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<IEnumerable<InvoiceProductDto>>;