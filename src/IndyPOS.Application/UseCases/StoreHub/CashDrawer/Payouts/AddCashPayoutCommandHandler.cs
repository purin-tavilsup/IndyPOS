using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

public class AddCashPayoutCommandHandler(
    ICashEntryRepository<CashPayout> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddCashPayoutCommand, CashPayoutDto>
{
    public async Task<CashPayoutDto> HandleAsync(AddCashPayoutCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var category = CashEntryRules.EnsureDefined(command.Category ?? PayoutCategory.General);
        var description = CashEntryRules.NormalizeDescription(command.Description);
        var now = clock.Now();

        var payout = new CashPayout
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            Category = category,
            Amount = command.Amount,
            Description = description,
            BusinessDate = now.BusinessDate,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(payout, CashDrawerOutbox.Changed(payout, now.Utc), cancellationToken);
        return payout.ToDto();
    }
}
