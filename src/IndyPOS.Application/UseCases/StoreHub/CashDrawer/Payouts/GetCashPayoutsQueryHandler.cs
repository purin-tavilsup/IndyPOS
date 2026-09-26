using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

public class GetCashPayoutsQueryHandler(
    ICashEntryRepository<CashPayout> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashPayoutsQuery, IReadOnlyList<CashPayoutDto>>
{
    public async Task<IReadOnlyList<CashPayoutDto>> HandleAsync(GetCashPayoutsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var payouts = await repository.ListAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return payouts.Select(p => p.ToDto()).ToList();
    }
}
