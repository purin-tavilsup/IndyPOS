using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>Same body and rules as <see cref="GetSaleByIdQuery"/>, keyed by the receipt's bill number.</summary>
public record GetSaleByNumberQuery(long InvoiceNumber, bool CanViewAnyDay) : IQuery<InvoiceDetailDto?>;
