﻿using AutoFixture.Xunit2;
using FluentAssertions;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Tests.Mocks.Attributes;
using IndyPOS.Application.UseCases.InventoryProducts;
using IndyPOS.Application.UseCases.InventoryProducts.Get;
using IndyPOS.Domain.Entities;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.InventoryProducts.Queries.GetInventoryProductsByCategoryId;

public class GetInventoryProductsByCategoryIdQueryHandlerTests
{
	[Theory]
	[CustomAutoData]
	public async Task Handle_ShouldReturnInventoryProducts(
		[Frozen] Mock<IInventoryProductRepository> productRepository,
		GetInventoryProductsByCategoryIdQueryHandler sut,
		int categoryId,
		InventoryProduct[] products)
	{
		// Arrange
		productRepository.Setup(x => x.GetProductsByCategoryId(categoryId))
						 .Returns(products);

		var query = new GetInventoryProductsByCategoryIdQuery(categoryId);

		// Act
		var results = await sut.Handle(query, default);

		// Assert
		results.Should().HaveSameCount(products)
			   .And.AllSatisfy(x => x.Should().BeOfType<InventoryProductDto>());
	}
}