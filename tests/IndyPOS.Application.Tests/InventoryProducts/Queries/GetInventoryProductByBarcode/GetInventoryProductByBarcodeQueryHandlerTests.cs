using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.InventoryProducts;
using IndyPOS.Application.InventoryProducts.Queries.GetByBarcode;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Domain.Entities;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.InventoryProducts.Queries.GetInventoryProductByBarcode;

public class GetInventoryProductByBarcodeQueryHandlerTests
{
	[Theory]
	[CustomAutoData]
    public async Task Handler_ShouldReturnInventoryProduct(
		[Frozen] Mock<IInventoryProductRepository> productRepository,
		GetInventoryProductByBarcodeQueryHandler sut,
		string barcode,
		InventoryProduct product)
	{
		// Arrange
		productRepository.Setup(x => x.GetByBarcode(barcode))
						 .Returns(product);

		var query = new GetInventoryProductByBarcodeQuery(barcode);

		// Act
		var result = await sut.Handle(query, default);

		// Assert
		result.Should().BeOfType<InventoryProductDto>()
			  .Which.Barcode.Should().Be(product.Barcode);
	}
}