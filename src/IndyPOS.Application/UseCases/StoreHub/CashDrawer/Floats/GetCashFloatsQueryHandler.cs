using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

public class GetCashFloatsQueryHandler(
    ICashEntryRepository<CashFloat> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashFloatsQuery, IReadOnlyList<CashFloatDto>>
{
    public async Task<IReadOnlyList<CashFloatDto>> HandleAsync(GetCashFloatsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var floats = await repository.ListAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return floats.Select(f => f.ToDto()).ToList();
    }
}
