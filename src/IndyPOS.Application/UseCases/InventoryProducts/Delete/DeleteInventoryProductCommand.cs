using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Delete;

public record DeleteInventoryProductCommand(int Id) : ICommand;