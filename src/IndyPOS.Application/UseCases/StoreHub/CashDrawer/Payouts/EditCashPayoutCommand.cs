using IndyPOS.Domain.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record EditCashPayoutCommand(Guid Id, Guid UserId, decimal Amount, PayoutCategory Category, string? Description)
    : ICommand<CashPayoutDto>;
