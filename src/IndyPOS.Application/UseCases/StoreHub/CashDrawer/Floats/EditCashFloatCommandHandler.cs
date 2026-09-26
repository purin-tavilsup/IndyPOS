using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <summary>Last save wins: no version check, by decision (one shared drawer).</summary>
public class EditCashFloatCommandHandler(
    ICashEntryRepository<CashFloat> repository,
    ICashDrawerClock clock) : ICommandHandler<EditCashFloatCommand, CashFloatDto>
{
    public async Task<CashFloatDto> HandleAsync(EditCashFloatCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var description = CashEntryRules.NormalizeDescription(command.Description);

        var cashFloat = await repository.FindAsync(command.Id, cancellationToken)
                        ?? throw new CashEntryNotFoundException(command.Id);
        var now = clock.Now();
        CashDayGuard.EnsureEditable(cashFloat.BusinessDate, now.BusinessDate);

        cashFloat.Amount = command.Amount;
        cashFloat.Description = description;
        cashFloat.Touch(command.UserId, now.Utc);

        await repository.SaveChangesAsync(CashDrawerOutbox.Changed(cashFloat, now.Utc), cancellationToken);
        return cashFloat.ToDto();
    }
}
