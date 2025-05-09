using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Get;

public record GetInvoiceProductsByDateRangeQuery(DateOnly StartDate, DateOnly EndDate) : IQuery<IEnumerable<InvoiceProductDto>>;