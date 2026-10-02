using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <summary>Null when unknown, or on another day and the caller lacks reports.view.</summary>
public record GetSaleByIdQuery(Guid InvoiceId, bool CanViewAnyDay) : IQuery<InvoiceDetailDto?>;
