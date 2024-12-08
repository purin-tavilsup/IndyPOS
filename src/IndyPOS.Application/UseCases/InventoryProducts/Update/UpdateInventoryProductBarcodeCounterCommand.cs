using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public record UpdateInventoryProductBarcodeCounterCommand(int Counter) : ICommand;