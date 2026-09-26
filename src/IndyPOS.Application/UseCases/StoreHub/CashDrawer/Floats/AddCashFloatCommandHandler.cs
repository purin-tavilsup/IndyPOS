using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

public class AddCashFloatCommandHandler(
    ICashEntryRepository<CashFloat> repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddCashFloatCommand, CashFloatDto>
{
    public async Task<CashFloatDto> HandleAsync(AddCashFloatCommand command, CancellationToken cancellationToken = default)
    {
        CashEntryRules.EnsureValidAmount(command.Amount);
        var description = CashEntryRules.NormalizeDescription(command.Description);
        var now = clock.Now();

        var cashFloat = new CashFloat
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            Amount = command.Amount,
            Description = description,
            BusinessDate = now.BusinessDate,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(cashFloat, CashDrawerOutbox.Changed(cashFloat, now.Utc), cancellationToken);
        return cashFloat.ToDto();
    }
}
