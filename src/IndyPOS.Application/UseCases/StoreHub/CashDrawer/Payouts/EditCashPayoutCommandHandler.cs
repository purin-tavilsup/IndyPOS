using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <summary>Last save wins: no version check, by decision (one shared drawer).</summary>
public class EditCashPayoutCommandHandler(
    ICashEntryRepository<CashPayout> repository,
    ICashDrawerClock clock) : ICommandHandler<EditCashPayoutCommand, CashPayoutDto>
{
    public async Task<CashPayoutDto> HandleAsync(EditCashPayoutCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var category = CashEntryRules.EnsureDefined(command.Category);
        var description = CashEntryRules.NormalizeDescription(command.Description);

        var payout = await repository.FindAsync(command.Id, cancellationToken)
                     ?? throw new CashEntryNotFoundException(command.Id);
        var now = clock.Now();
        CashDayGuard.EnsureEditable(payout.BusinessDate, now.BusinessDate);

        payout.Amount = command.Amount;
        payout.Category = category;
        payout.Description = description;
        payout.Touch(command.UserId, now.Utc);

        await repository.SaveChangesAsync(CashDrawerOutbox.Changed(payout, now.Utc), cancellationToken);
        return payout.ToDto();
    }
}
