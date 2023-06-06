using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Commands.DeleteInventoryProduct;

public record DeleteInventoryProductCommand(int Id) : ICommand;