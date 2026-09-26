using IndyPOS.Domain.Enums;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Payouts;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
/// <param name="Category">Null means General.</param>
public record AddCashPayoutCommand(Guid UserId, decimal Amount, PayoutCategory? Category, string? Description)
    : ICommand<CashPayoutDto>;
