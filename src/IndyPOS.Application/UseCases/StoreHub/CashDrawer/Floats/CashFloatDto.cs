using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

public record CashFloatDto(
    Guid Id,
    decimal Amount,
    string? Description,
    DateOnly BusinessDate,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc,
    Guid CreatedByUserId,
    Guid? LastModifiedByUserId);

public static class CashFloatMapping
{
    public static CashFloatDto ToDto(this CashFloat cashFloat) => new(
        cashFloat.Id,
        cashFloat.Amount,
        cashFloat.Description,
        cashFloat.BusinessDate,
        cashFloat.CreatedUtc,
        cashFloat.LastModifiedUtc,
        cashFloat.CreatedByUserId,
        cashFloat.LastModifiedByUserId);
}
