using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.UseCases.StoreHub.CashDrawer.Common;
using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <summary>Every count is a new row; nothing is overwritten. A mistyped count is fixed by counting again.</summary>
public class AddCashCountCommandHandler(
    ICashCountRepository repository,
    ICashDrawerClock clock,
    IStoreIdentityService storeIdentity) : ICommandHandler<AddCashCountCommand, CashCountDto>
{
    public async Task<CashCountDto> HandleAsync(AddCashCountCommand command, CancellationToken cancellationToken = default)
    {
        var c = command.Counts;
        CashEntryRules.EnsureValidCounts(
            c.BankNote1000Count, c.BankNote500Count, c.BankNote100Count, c.BankNote50Count, c.BankNote20Count,
            c.Coin10Count, c.Coin5Count, c.Coin2Count, c.Coin1Count);
        var now = clock.Now();

        var count = new CashCount
        {
            Id = Guid.NewGuid(),
            StoreId = storeIdentity.StoreId,
            BusinessDate = now.BusinessDate,
            BankNote1000Count = c.BankNote1000Count,
            BankNote500Count = c.BankNote500Count,
            BankNote100Count = c.BankNote100Count,
            BankNote50Count = c.BankNote50Count,
            BankNote20Count = c.BankNote20Count,
            Coin10Count = c.Coin10Count,
            Coin5Count = c.Coin5Count,
            Coin2Count = c.Coin2Count,
            Coin1Count = c.Coin1Count,
            CreatedUtc = now.Utc,
            LastModifiedUtc = now.Utc,
            CreatedByUserId = command.UserId
        };

        await repository.AddAsync(count, CashDrawerOutbox.CountAdded(count, now.Utc), cancellationToken);
        return count.ToDto();
    }
}
