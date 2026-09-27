using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <summary>
/// Last save wins against another edit: no version check, by decision (one shared drawer). An
/// entry deleted after it was loaded is not found, the same as one deleted before.
/// </summary>
public class EditDebtRepaymentCommandHandler(
    ICashEntryRepository<DebtRepayment> repository,
    ICashDrawerClock clock) : ICommandHandler<EditDebtRepaymentCommand, DebtRepaymentDto>
{
    public async Task<DebtRepaymentDto> HandleAsync(EditDebtRepaymentCommand command, CancellationToken cancellationToken = default)
    {
        var customerName = CashEntryRules.NormalizeCustomerName(command.CustomerName);
        CashEntryRules.EnsureValidAmount(command.Amount);

        var repayment = await repository.FindAsync(command.Id, cancellationToken)
                        ?? throw new CashEntryNotFoundException(command.Id);
        var now = clock.Now();
        CashDayGuard.EnsureEditable(repayment.BusinessDate, now.BusinessDate);

        repayment.CustomerName = customerName;
        repayment.Amount = command.Amount;
        repayment.Touch(command.UserId, now.Utc);

        if (!await repository.TrySaveChangesAsync(CashDrawerOutbox.Changed(repayment, now.Utc), cancellationToken))
            throw new CashEntryNotFoundException(command.Id);

        return repayment.ToDto();
    }
}
