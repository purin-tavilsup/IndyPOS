using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

public record CashCountDto(
    Guid Id,
    DateOnly BusinessDate,
    int BankNote1000Count,
    int BankNote500Count,
    int BankNote100Count,
    int BankNote50Count,
    int BankNote20Count,
    int Coin10Count,
    int Coin5Count,
    int Coin2Count,
    int Coin1Count,
    decimal CountedTotal,
    DateTime CreatedUtc,
    Guid CreatedByUserId);

public static class CashCountMapping
{
    public static CashCountDto ToDto(this CashCount count) => new(
        count.Id,
        count.BusinessDate,
        count.BankNote1000Count,
        count.BankNote500Count,
        count.BankNote100Count,
        count.BankNote50Count,
        count.BankNote20Count,
        count.Coin10Count,
        count.Coin5Count,
        count.Coin2Count,
        count.Coin1Count,
        count.CountedTotal,
        count.CreatedUtc,
        count.CreatedByUserId);
}
