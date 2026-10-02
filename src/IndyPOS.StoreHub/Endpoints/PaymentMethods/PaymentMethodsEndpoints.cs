using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.PaymentMethods;

/// <summary>
/// Payment methods under two roots: /payment-methods is what the till may offer, and
/// /admin/payment-methods is the catalogue an admin manages.
/// </summary>
public static class PaymentMethodsEndpoints
{
    public static IEndpointRouteBuilder MapPaymentMethodsEndpoints(this IEndpointRouteBuilder app)
    {
        MapOfferable(app);
        MapCatalogue(app);
        MapAddCampaign(app);
        MapUpdateMethod(app);
        return app;
    }

    // Payment methods endpoint (offerable methods for this store)
    private static void MapOfferable(IEndpointRouteBuilder app)
    {
        app.MapGet("/payment-methods", async (
            IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var methods = await handler.HandleAsync(new GetOfferablePaymentMethodsQuery(), cancellationToken);
            return Results.Ok(methods);
        }).RequireAuthorization("CanReadProducts");
    }

    // Admin: list all payment methods (enabled + disabled)
    private static void MapCatalogue(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/payment-methods", async (
            IQueryHandler<GetAllPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> handler,
            CancellationToken cancellationToken) =>
        {
            var methods = await handler.HandleAsync(new GetAllPaymentMethodsQuery(), cancellationToken);
            return Results.Ok(methods);
        }).RequireAuthorization("CanManagePaymentMethods");
    }

    // Admin: add a government-campaign payment method
    private static void MapAddCampaign(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/payment-methods", async (
            ICommandHandler<AddCampaignPaymentMethodCommand, PaymentMethodMutationResponse> handler,
            AddCampaignPaymentMethodRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new AddCampaignPaymentMethodCommand(request.Code, request.DisplayName, request.DisplayOrder);
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManagePaymentMethods");
    }

    // Admin: toggle enabled state and/or edit display of a payment method
    private static void MapUpdateMethod(IEndpointRouteBuilder app)
    {
        app.MapPatch("/admin/payment-methods/{code}", async (
            ICommandHandler<TogglePaymentMethodCommand, PaymentMethodMutationResponse> toggleHandler,
            ICommandHandler<EditPaymentMethodDisplayCommand, PaymentMethodMutationResponse> editHandler,
            string code,
            UpdatePaymentMethodRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                if (request.IsEnabled is bool enabled)
                {
                    await toggleHandler.HandleAsync(new TogglePaymentMethodCommand(code, enabled), cancellationToken);
                }

                if (request.DisplayName is not null)
                {
                    await editHandler.HandleAsync(
                        new EditPaymentMethodDisplayCommand(code, request.DisplayName, request.DisplayOrder),
                        cancellationToken);
                }

                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        }).RequireAuthorization("CanManagePaymentMethods");
    }
}
