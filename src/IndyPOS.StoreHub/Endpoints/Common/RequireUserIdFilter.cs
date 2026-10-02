namespace IndyPOS.StoreHub.Endpoints.Common;

/// <summary>
/// Rejects a token without a usable user id up front (401), before the route runs. Add it to every
/// route or group that stamps the acting user on a write with
/// <see cref="ClaimsPrincipalExtensions.GetRequiredUserId"/>, so such a token cannot get as far as a
/// 500 from that call. The /cash group also guards its reads with it, so a token without an identity
/// never reaches cash data at all.
/// </summary>
internal sealed class RequireUserIdFilter : IEndpointFilter
{
    public ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) =>
        context.HttpContext.User.FindUserId() is null
            ? ValueTask.FromResult<object?>(Results.Unauthorized())
            : next(context);
}
