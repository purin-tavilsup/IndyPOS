using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

public record CashPayoutDto(
    Guid Id,
    PayoutCategory Category,
    decimal Amount,
    string? Description,
    DateOnly BusinessDate,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc,
    Guid CreatedByUserId,
    Guid? LastModifiedByUserId);

public static class CashPayoutMapping
{
    public static CashPayoutDto ToDto(this CashPayout payout) => new(
        payout.Id,
        payout.Category,
        payout.Amount,
        payout.Description,
        payout.BusinessDate,
        payout.CreatedUtc,
        payout.LastModifiedUtc,
        payout.CreatedByUserId,
        payout.LastModifiedByUserId);
}
