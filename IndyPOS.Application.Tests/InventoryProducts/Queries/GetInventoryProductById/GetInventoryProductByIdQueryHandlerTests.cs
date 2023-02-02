using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.InventoryProducts.Queries;
using IndyPOS.Application.InventoryProducts.Queries.GetInventoryProductById;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Domain.Entities;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.InventoryProducts.Queries.GetInventoryProductById;

public class GetInventoryProductByIdQueryHandlerTests
{
	[Theory]
	[CustomAutoData]
	public async Task Handler_ShouldReturnInventoryProduct(
		[Frozen] Mock<IInventoryProductRepository> productRepository,
		GetInventoryProductByIdQueryHandler sut,
		int id,
		InventoryProduct product)
	{
		// Arrange
		productRepository.Setup(x => x.GetById(id))
						 .Returns(product);

		var query = new GetInventoryProductByIdQuery(id);

		// Act
		var result = await sut.Handle(query, default);

		// Assert
		result.Should().BeOfType<InventoryProductDto>()
			  .Which.InventoryProductId.Should().Be(product.InventoryProductId);
	}
}