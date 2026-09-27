using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <param name="BusinessDate">Null means today (store timezone). Past days are viewable, not editable.</param>
public record GetCashFloatsQuery(DateOnly? BusinessDate) : IQuery<IReadOnlyList<CashFloatDto>>;
