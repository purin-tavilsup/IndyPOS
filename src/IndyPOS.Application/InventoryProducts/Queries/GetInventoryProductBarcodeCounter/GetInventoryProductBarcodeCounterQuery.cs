using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductBarcodeCounter;

public record GetInventoryProductBarcodeCounterQuery() : IQuery<int>;