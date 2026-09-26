using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

public class GetDebtRepaymentsQueryHandler(
    ICashEntryRepository<DebtRepayment> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : IQueryHandler<GetDebtRepaymentsQuery, IReadOnlyList<DebtRepaymentDto>>
{
    public async Task<IReadOnlyList<DebtRepaymentDto>> HandleAsync(GetDebtRepaymentsQuery query, CancellationToken cancellationToken = default)
    {
        var businessDate = query.BusinessDate ?? clock.Now().BusinessDate;
        var repayments = await repository.ListAsync(storeIdentity.StoreId, businessDate, cancellationToken);
        return repayments.Select(r => r.ToDto()).ToList();
    }
}
