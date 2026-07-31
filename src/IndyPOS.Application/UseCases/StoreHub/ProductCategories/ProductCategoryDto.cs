using IndyPOS.Domain.Enums;

namespace IndyPOS.Application.UseCases.StoreHub.ProductCategories;

public record ProductCategoryDto(
    string Code, string DisplayName, ProductCategoryKind Kind, bool IsEnabled, int DisplayOrder);
