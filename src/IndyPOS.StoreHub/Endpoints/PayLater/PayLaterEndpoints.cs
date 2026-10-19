using System.Security.Claims;
using IndyPOS.Application.Common.Exceptions;
using IndyPOS.Application.UseCases.StoreHub.PayLater;
using IndyPOS.StoreHub.Endpoints.Common;
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

    // List pay-later records; createdOn keeps only the debts taken on that store day, completed ones too whatever
    // includeCompleted says (the cash drawer's ลงบัญชี list).
    private static void MapListDebts(IEndpointRouteBuilder app)
    {
        app.MapGet("/pay-later", async (
            IQueryHandler<GetPayLaterQuery, GetPayLaterResponse> handler,
            bool? includeCompleted,
            string? search,
            DateOnly? createdOn,
            CancellationToken cancellationToken) =>
        {
            var query = new GetPayLaterQuery(
                IncludeCompleted: includeCompleted ?? false,
                SearchTerm: search,
                CreatedOn: createdOn);

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

    // Record a repayment against a pay-later debt; the repayment names its cashier, so a token must carry a user id
    private static void MapRecordPayment(IEndpointRouteBuilder app)
    {
        app.MapPost("/pay-later/{id:guid}/payments", async (
            ICommandHandler<RecordPayLaterPaymentCommand, PayLaterDto> handler,
            Guid id,
            RecordPaymentRequest request,
            ClaimsPrincipal user,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var command = new RecordPayLaterPaymentCommand(id, request.PaymentAmount, user.GetRequiredUserId());
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
            catch (PayLaterPaymentConflictException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
        }).RequireAuthorization()
          .AddEndpointFilter<RequireUserIdFilter>();
    }
}
