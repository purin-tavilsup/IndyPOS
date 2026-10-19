using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

public class GetCashCountsQueryHandler(
    ICashCountRepository repository,
    IStoreUserRepository users,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetCashCountsQuery, IReadOnlyList<CashCountDto>>
{
    public async Task<IReadOnlyList<CashCountDto>> HandleAsync(GetCashCountsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var counts = await repository.ListNewestFirstAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        var names = await NamesAsync(counts.Select(c => c.CreatedByUserId).Distinct(), cancellationToken);
        return counts.Select(c => c.ToDto(names[c.CreatedByUserId])).ToList();
    }

    // A handful of cashiers a day, so one lookup each is fine.
    private async Task<Dictionary<Guid, string?>> NamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string?>();
        foreach (var id in userIds)
            names[id] = (await users.GetByIdAsync(id, cancellationToken)).DisplayName();
        return names;
    }
}
