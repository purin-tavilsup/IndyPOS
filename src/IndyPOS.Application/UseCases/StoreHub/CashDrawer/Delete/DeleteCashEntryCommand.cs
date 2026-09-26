using IndyPOS.Domain.Entities.Core;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.CashDrawer.Delete;

/// <param name="UserId">The acting cashier, taken from the login token by the endpoint.</param>
public record DeleteCashEntryCommand<TEntry>(Guid Id, Guid UserId) : ICommand
    where TEntry : CashDrawerEntry;
