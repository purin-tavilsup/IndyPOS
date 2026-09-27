using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

public class AddDebtRepaymentCommandHandler(
    ICashEntryRepository<DebtRepayment> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddDebtRepaymentCommand, DebtRepaymentDto>
{
    public async Task<DebtRepaymentDto> HandleAsync(AddDebtRepaymentCommand command, CancellationToken cancellationToken = default)
    {
        var customerName = CashEntryRules.NormalizeCustomerName(command.CustomerName);
        CashEntryRules.EnsureValidAmount(command.Amount);
        var now = clock.Now();

        var repayment = new DebtRepayment
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            CustomerName = customerName,
            Amount = command.Amount,
            BusinessDate = now.BusinessDate,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(repayment, CashDrawerOutbox.Changed(repayment, now.Utc), cancellationToken);
        return repayment.ToDto();
    }
}
