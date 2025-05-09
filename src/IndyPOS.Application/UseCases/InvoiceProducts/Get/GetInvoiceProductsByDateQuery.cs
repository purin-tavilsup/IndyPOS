using Nokpirab;

namespace IndyPOS.Application.UseCases.InvoiceProducts.Get;

public record GetInvoiceProductsByDateQuery(DateOnly Date) : IQuery<IEnumerable<InvoiceProductDto>>;