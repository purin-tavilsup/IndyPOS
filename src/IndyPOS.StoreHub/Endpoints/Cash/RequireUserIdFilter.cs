namespace IndyPOS.StoreHub.Endpoints.Cash;

/// <summary>
/// Guards the whole /cash group — reads included, not just writes. Every cash write is stamped
/// with the acting user, so a token without a usable user id is rejected up front (401) instead of
/// failing later as a 500; reads are rejected too, so a token without an identity never reaches
/// cash data at all.
/// </summary>
internal sealed class RequireUserIdFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.User.FindUserId() is null
            ? ValueTask.FromResult<object?>(Results.Unauthorized())
            : next(context);
}
