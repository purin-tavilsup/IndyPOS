using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.StoreHub.Endpoints.Sales;

/// <summary>Maps /sales rule failures to HTTP, in the { error } shape every StoreHub route uses.</summary>
internal sealed class SalesExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (SalesQueryValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (SaleNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (OtherDayForbiddenException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status403Forbidden);
        }
    }
}
