using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Commands.Delete;

public record DeleteInventoryProductCommand(int Id) : ICommand;