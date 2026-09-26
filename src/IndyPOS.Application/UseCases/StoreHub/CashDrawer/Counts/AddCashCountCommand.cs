using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Counts;

/// <param name="UserId">Who counted, taken from the login token by the endpoint.</param>
public record AddCashCountCommand(Guid UserId, AddCashCountRequest Counts) : ICommand<CashCountDto>;
