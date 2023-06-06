using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductByBarcode;

public record GetInventoryProductByBarcodeQuery(string Barcode) : IQuery<InventoryProductDto>;