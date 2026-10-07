using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.PaymentMethods;
using Microsoft.AspNetCore.Authorization;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.PaymentMethods;

/// <summary>
/// Payment methods under one root. GET /payment-methods is what the till may offer;
/// ?include=all widens it to the whole catalogue, enabled or not. The catalogue, adding a campaign
/// and editing a method all need CanManagePaymentMethods.
/// </summary>
public static class PaymentMethodsEndpoints
{
    private const string ManagePolicy = "CanManagePaymentMethods";
    private const string IncludeAll = "all";

    public static IEndpointRouteBuilder MapPaymentMethodsEndpoints(this IEndpointRouteBuilder app)
    {
        MapList(app);
        MapAddCampaign(app);
        MapUpdateMethod(app);
        return app;
    }

    internal static string UnknownIncludeMessage(string value) =>
        $"ค่า include ไม่ถูกต้อง: {value} (ใช้ได้เฉพาะ all)";

    // The route carries the weaker policy so a cashier still reaches the offerable list. The
    // catalogue's stronger check runs here, and only when the catalogue is asked for.
    private static void MapList(IEndpointRouteBuilder app)
    {
        app.MapGet("/payment-methods", async (
            IQueryHandler<GetOfferablePaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> offerable,
            IQueryHandler<GetAllPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>> catalogue,
            IAuthorizationService authorization,
            ClaimsPrincipal user,
            string? include,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrEmpty(include))
            {
                return Results.Ok(await offerable.HandleAsync(new GetOfferablePaymentMethodsQuery(), cancellationToken));
            }

            if (!string.Equals(include, IncludeAll, StringComparison.Ordinal))
            {
                return Results.BadRequest(new { error = UnknownIncludeMessage(include) });
            }

            var canManage = await authorization.AuthorizeAsync(user, ManagePolicy);
            return canManage.Succeeded
                ? Results.Ok(await catalogue.HandleAsync(new GetAllPaymentMethodsQuery(), cancellationToken))
                : Results.Forbid();
        }).RequireAuthorization("CanReadProducts");
    }

    // Add a government-campaign payment method
    private static void MapAddCampaign(IEndpointRouteBuilder app)
    {
        app.MapPost("/payment-methods", async (
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
        }).RequireAuthorization(ManagePolicy);
    }

    // Toggle enabled state and/or edit display of a payment method
    private static void MapUpdateMethod(IEndpointRouteBuilder app)
    {
        app.MapPatch("/payment-methods/{code}", async (
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
        }).RequireAuthorization(ManagePolicy);
    }
}
