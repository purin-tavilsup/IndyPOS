using IndyPOS.Application.Common.Exceptions;

namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>Maps cash-drawer rule failures to HTTP, in the { error } shape every StoreHub route uses.</summary>
internal sealed class CashExceptionFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (CashEntryValidationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (CashEntryNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (CashDayClosedException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
