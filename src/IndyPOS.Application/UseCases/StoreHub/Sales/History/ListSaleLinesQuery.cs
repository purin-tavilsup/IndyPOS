using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Sales.History;

/// <param name="From">Null = today's business date.</param>
/// <param name="To">Null = today's business date.</param>
/// <param name="CanViewAnyDay">The caller holds reports.view; otherwise both dates must be today.</param>
public record ListSaleLinesQuery(DateOnly? From, DateOnly? To, int Page, int PageSize, bool CanViewAnyDay)
    : IQuery<SaleLinesPage>;
