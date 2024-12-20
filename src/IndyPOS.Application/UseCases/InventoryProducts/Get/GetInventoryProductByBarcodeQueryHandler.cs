﻿using IndyPOS.Application.Abstractions.Messaging;
using IndyPOS.Application.Abstractions.Pos.Repositories;

namespace IndyPOS.Application.UseCases.InventoryProducts.Get;

public class GetInventoryProductByBarcodeQueryHandler : IQueryHandler<GetInventoryProductByBarcodeQuery, InventoryProductDto>
{
	private readonly IInventoryProductRepository _productRepository;

	public GetInventoryProductByBarcodeQueryHandler(IInventoryProductRepository productRepository)
	{
		_productRepository = productRepository;
	}

	public Task<InventoryProductDto> Handle(GetInventoryProductByBarcodeQuery query, CancellationToken cancellationToken)
	{
		var barcode = query.Barcode;
		var result = _productRepository.GetByBarcode(barcode);

		return Task.FromResult(result.ToDto());
	}
}