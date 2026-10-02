using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.ProductCategories;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Catalogue;

/// <summary>What this store sells and how: its product categories and its store-type feature flags.</summary>
public static class CatalogueEndpoints
{
    public static IEndpointRouteBuilder MapCatalogueEndpoints(this IEndpointRouteBuilder app)
    {
        MapProductCategories(app);
        MapStoreFeatures(app);
        return app;
    }

    // Product categories for this store (the POS renders pickers from this)
    private static void MapProductCategories(IEndpointRouteBuilder app)
    {
        app.MapGet("/product-categories", async (
            IQueryHandler<GetProductCategoriesQuery, IReadOnlyList<ProductCategoryDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var categories = await handler.HandleAsync(new GetProductCategoriesQuery(), cancellationToken);
            return Results.Ok(categories);
        }).RequireAuthorization("CanReadProducts");
    }

    // Store feature flags (store-type gating for WinForms clients)
    private static void MapStoreFeatures(IEndpointRouteBuilder app)
    {
        app.MapGet("/store/features", (IStoreIdentityService storeIdentity) =>
        {
            var f = storeIdentity.Features;
            return Results.Ok(new StoreFeaturesDto(f.PayLaterEnabled, f.MultipleProductTypesEnabled));
        }).RequireAuthorization();
    }
}
