using System.Security.Claims;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.Products;
using IndyPOS.Application.UseCases.StoreHub.Products.AdjustQuantity;
using IndyPOS.Application.UseCases.StoreHub.Products.Create;
using IndyPOS.Application.UseCases.StoreHub.Products.Delete;
using IndyPOS.Application.UseCases.StoreHub.Products.GenerateBarcode;
using IndyPOS.Application.UseCases.StoreHub.Products.Get;
using IndyPOS.Application.UseCases.StoreHub.Products.GetStock;
using IndyPOS.Application.UseCases.StoreHub.Products.Update;
using IndyPOS.StoreHub.Endpoints.Common;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Products;

/// <summary>The product catalogue, its stock balances, stock adjustments and barcode numbering.</summary>
public static class ProductsEndpoints
{
    public static IEndpointRouteBuilder MapProductsEndpoints(this IEndpointRouteBuilder app)
    {
        MapListProducts(app);
        MapProductStock(app);
        MapCreateProduct(app);
        MapUpdateProduct(app);
        MapDeleteProduct(app);
        MapAdjustQuantity(app);
        MapNextBarcode(app);
        return app;
    }

    private static void MapListProducts(IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (
            IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>> handler,
            bool? activeOnly,
            string? category,
            string? search,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProductsQuery(
                ActiveOnly: activeOnly ?? true,
                Category: category,
                SearchTerm: search);

            var products = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(products);
        }).RequireAuthorization("CanReadProducts");
    }

    // Current stock per product. Separate from /products on purpose: the POS caches
    // products for the session, and a quantity on that record would go stale at the
    // first sale on either terminal.
    private static void MapProductStock(IEndpointRouteBuilder app)
    {
        app.MapGet("/products/stock", async (
            IQueryHandler<GetProductStockQuery, IReadOnlyList<ProductStockDto>> handler,
            Guid? productId,
            CancellationToken cancellationToken) =>
        {
            var stock = await handler.HandleAsync(new GetProductStockQuery(productId), cancellationToken);
            return Results.Ok(stock);
        }).RequireAuthorization("CanReadProducts");
    }

    private static void MapCreateProduct(IEndpointRouteBuilder app)
    {
        app.MapPost("/products", async (
            ICommandHandler<CreateProductCommand, ProductDto> handler,
            CreateProductCommand command,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Created($"/products/{result.Id}", result);
            }
            catch (UnknownProductCategoryException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManageProducts");
    }

    private static void MapUpdateProduct(IEndpointRouteBuilder app)
    {
        app.MapPut("/products/{id:guid}", async (
            ICommandHandler<UpdateProductCommand, ProductDto> handler,
            Guid id,
            UpdateProductCommand command,
            CancellationToken cancellationToken) =>
        {
            // Ensure ID matches
            if (id != command.Id)
            {
                return Results.BadRequest("Product ID in URL does not match body");
            }

            try
            {
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (ProductNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
            catch (UnknownProductCategoryException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Duplicate barcode or a store-type violation — both genuine conflicts.
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManageProducts");
    }

    // Soft delete
    private static void MapDeleteProduct(IEndpointRouteBuilder app)
    {
        app.MapDelete("/products/{id:guid}", async (
            ICommandHandler<DeleteProductCommand> handler,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            await handler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
            return Results.NoContent();
        }).RequireAuthorization("CanManageProducts");
    }

    // Record a stock adjustment: a signed delta. The adjuster is the token's user, never the body's.
    private static void MapAdjustQuantity(IEndpointRouteBuilder app)
    {
        app.MapPost("/products/{id:guid}/stock-adjustments", async (
            ICommandHandler<AdjustProductQuantityCommand, int> handler,
            ClaimsPrincipal user,
            Guid id,
            AdjustQuantityRequest request,
            CancellationToken cancellationToken) =>
        {
            if (request.Delta == 0)
            {
                return Results.BadRequest(new { error = "Delta must not be zero." });
            }

            var command = new AdjustProductQuantityCommand
            {
                ProductId = id,
                Delta = request.Delta,
                Reason = request.Reason,
                UserId = user.GetRequiredUserId()
            };

            var newBalance = await handler.HandleAsync(command, cancellationToken);
            return Results.Ok(new AdjustQuantityResponse(id, newBalance));
        }).RequireAuthorization("CanAdjustInventory")
          .AddEndpointFilter<RequireUserIdFilter>();
    }

    private static void MapNextBarcode(IEndpointRouteBuilder app)
    {
        app.MapPost("/products/next-barcode", async (
            IQueryHandler<GenerateBarcodeQuery, string> handler,
            CancellationToken cancellationToken) =>
        {
            var barcode = await handler.HandleAsync(new GenerateBarcodeQuery(), cancellationToken);
            return Results.Ok(new { barcode });
        }).RequireAuthorization("CanManageProducts");
    }
}
