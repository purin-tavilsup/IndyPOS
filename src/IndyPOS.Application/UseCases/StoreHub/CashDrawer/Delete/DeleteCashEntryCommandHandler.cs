using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;

/// <summary>
/// Soft-deletes a payout, float or debt repayment. Idempotent: a repeat delete (double-click, retry
/// after a network blip) changes nothing and writes no event, so the audit record is never rewritten.
/// That holds for two deletes in flight at once too: the one that saves second finds the row
/// already deleted and is a no-op.
/// </summary>
public class DeleteCashEntryCommandHandler<TEntry>(
    ICashEntryRepository<TEntry> repository,
    ICashDrawerClock clock) : ICommandHandler<DeleteCashEntryCommand<TEntry>>
    where TEntry : CashDrawerEntry
{
    public async Task HandleAsync(DeleteCashEntryCommand<TEntry> command, CancellationToken cancellationToken = default)
    {
        var entry = await repository.FindIncludingDeletedAsync(command.Id, cancellationToken)
                    ?? throw new CashEntryNotFoundException(command.Id);

        // Before the day rule on purpose: re-deleting any already-deleted row is a harmless no-op.
        if (entry.IsDeleted)
            return;

        var now = clock.Now();
        CashDayGuard.EnsureEditable(entry.BusinessDate, now.BusinessDate);

        entry.MarkDeleted(command.UserId, now.Utc);
        // False means a concurrent delete won; that is the idempotent outcome, so nothing to do.
        await repository.TrySaveChangesAsync(CashDrawerOutbox.Changed(entry, now.Utc), cancellationToken);
    }
}
