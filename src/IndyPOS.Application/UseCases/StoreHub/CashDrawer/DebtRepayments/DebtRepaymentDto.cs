using IndyPOS.Domain.Entities.Core;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

public record DebtRepaymentDto(
    Guid Id,
    string CustomerName,
    decimal Amount,
    DateOnly BusinessDate,
    DateTime CreatedUtc,
    DateTime LastModifiedUtc,
    Guid CreatedByUserId,
    Guid? LastModifiedByUserId);

public static class DebtRepaymentMapping
{
    public static DebtRepaymentDto ToDto(this DebtRepayment repayment) => new(
        repayment.Id,
        repayment.CustomerName,
        repayment.Amount,
        repayment.BusinessDate,
        repayment.CreatedUtc,
        repayment.LastModifiedUtc,
        repayment.CreatedByUserId,
        repayment.LastModifiedByUserId);
}
