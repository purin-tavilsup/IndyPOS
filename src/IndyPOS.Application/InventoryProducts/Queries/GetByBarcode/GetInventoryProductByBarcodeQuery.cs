using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetByBarcode;

public record GetInventoryProductByBarcodeQuery(string Barcode) : IQuery<InventoryProductDto>;