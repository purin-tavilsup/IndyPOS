using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Commands.UpdateInventoryProductBarcodeCounter;

public record UpdateInventoryProductBarcodeCounterCommand(int Counter) : ICommand;