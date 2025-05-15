using Nokpirab;

namespace IndyPOS.Application.UseCases.InventoryProducts.Update;

public record UpdateInventoryProductBarcodeCounterCommand(int Counter) : ICommand;