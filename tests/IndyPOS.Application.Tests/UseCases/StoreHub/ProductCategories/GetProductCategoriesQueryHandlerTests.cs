using FluentAssertions;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Domain.Enums;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.UseCases.StoreHub.ProductCategories;

public class GetProductCategoriesQueryHandlerTests
{
    private static ProductCategory Row(string code, int order, bool enabled = true) => new()
    {
        StoreId = "STORE-A",
        Code = code,
        DisplayName = code,
        Kind = ProductCategoryKind.GeneralGoods,
        IsEnabled = enabled,
        DisplayOrder = order,
        CreatedUtc = DateTime.UtcNow,
        LastModifiedUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task HandleAsync_ShouldReturnDisabledCategoriesToo()
    {
        // The POS filters for pickers; existing products referencing a disabled category must
        // still render their label, so the endpoint returns everything with its IsEnabled flag.
        var repository = new Mock<IProductCategoryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync([Row("Gifts", 1), Row("Retired", 2, enabled: false)]);

        var result = await new GetProductCategoriesQueryHandler(repository.Object)
            .HandleAsync(new GetProductCategoriesQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result.Single(c => c.Code == "Retired").IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAsync_ShouldPreserveTheRepositoryOrdering()
    {
        var repository = new Mock<IProductCategoryRepository>();
        repository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync([Row("First", 1), Row("Second", 2)]);

        var result = await new GetProductCategoriesQueryHandler(repository.Object)
            .HandleAsync(new GetProductCategoriesQuery(), CancellationToken.None);

        result.Select(c => c.Code).Should().Equal("First", "Second");
    }
}
