using IndyPOS.Application.Abstractions.Messaging;

namespace IndyPOS.Application.InventoryProducts.Queries.GetBarcodeCounter;

public record GetInventoryProductBarcodeCounterQuery() : IQuery<int>;