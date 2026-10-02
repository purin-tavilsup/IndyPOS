using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.PayLater;

/// <summary>PayLater accounts: for cashiers to view and update the debts customers carry.</summary>
public static class PayLaterEndpoints
{
    public static IEndpointRouteBuilder MapPayLaterEndpoints(this IEndpointRouteBuilder app)
    {
        MapListDebts(app);
        MapDebtById(app);
        MapRecordPayment(app);
        return app;
    }

    // List pay-later records
    private static void MapListDebts(IEndpointRouteBuilder app)
    {
        app.MapGet("/pay-later", async (
            IQueryHandler<GetPayLaterQuery, GetPayLaterResponse> handler,
            bool? includeCompleted,
            string? search,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPayLaterQuery(
                IncludeCompleted: includeCompleted ?? false,
                SearchTerm: search);

            var result = await handler.HandleAsync(query, cancellationToken);
            return Results.Ok(result);
        }).RequireAuthorization();
    }

    // Get single pay-later record
    private static void MapDebtById(IEndpointRouteBuilder app)
    {
        app.MapGet("/pay-later/{id:guid}", async (
            IQueryHandler<GetPayLaterByIdQuery, PayLaterDto> handler,
            Guid id,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var result = await handler.HandleAsync(new GetPayLaterByIdQuery(id), cancellationToken);
                return Results.Ok(result);
            }
            catch (PayLaterPaymentNotFoundException)
            {
                return Results.NotFound();
            }
        }).RequireAuthorization();
    }

    // Record payment against pay-later
    private static void MapRecordPayment(IEndpointRouteBuilder app)
    {
        app.MapPost("/pay-later/{id:guid}/record-payment", async (
            ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto> handler,
            Guid id,
            RecordPaymentRequest request,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new RecordPayLaterPaymentCommand(id, request.PaymentAmount);
                var result = await handler.HandleAsync(command, cancellationToken);
                return Results.Ok(result);
            }
            catch (PayLaterPaymentNotFoundException)
            {
                return Results.NotFound();
            }
            catch (PayLaterPaymentNotUpdatedException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization();
    }
}
