using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public record GetInventoryProductByBarcodeQuery(string Barcode) : IQuery<InventoryProductDto>;