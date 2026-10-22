namespace McpSignIn;

/// <summary>Logs every request, so the findings can say exactly which path each client took.</summary>
public sealed class RequestLogMiddleware(RequestDelegate next, ILogger<RequestLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);
        logger.LogWarning("{Method} {Path}{Query} -> {Status} (UA: {UserAgent})",
                          context.Request.Method, context.Request.Path, context.Request.QueryString,
                          context.Response.StatusCode, context.Request.Headers.UserAgent.ToString());
    }
}
