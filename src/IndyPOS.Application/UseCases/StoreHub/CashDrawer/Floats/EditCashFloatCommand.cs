using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Floats;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record EditCashFloatCommand(Guid Id, Guid UserId, decimal Amount, string? Description) : ICommand<CashFloatDto>;
