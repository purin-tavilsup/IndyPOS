using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.DebtRepayments;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record EditDebtRepaymentCommand(Guid Id, Guid UserId, string? CustomerName, decimal Amount) : ICommand<DebtRepaymentDto>;
