using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

public class GetCashCountsQueryHandler(
    ICashCountRepository repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashCountsQuery, IReadOnlyList<CashCountDto>>
{
    public async Task<IReadOnlyList<CashCountDto>> HandleAsync(GetCashCountsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var counts = await repository.ListNewestFirstAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return counts.Select(c => c.ToDto()).ToList();
    }
}
