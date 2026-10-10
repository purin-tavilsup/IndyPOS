using System.Security.Claims;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Application.UseCases.StoreHub.Auth.Login;
using Nokpirab;

namespace IndyPOS.StoreHub.Endpoints.Auth;

/// <summary>
/// Sign-in and the signed-in user. The must_change gate in Program.cs matches /auth/change-password
/// by path: a token that must change its password may reach that route and no other.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        MapLogin(app);
        MapCurrentUser(app);
        MapChangePassword(app);
        return app;
    }

    private static void MapLogin(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/login", async (
            ICommandHandler<LoginCommand, LoginResponse> handler,
            LoginRequest request,
            CancellationToken cancellationToken) =>
        {
            var command = new LoginCommand(request.Username, request.Password);
            var response = await handler.HandleAsync(command, cancellationToken);

            return response.Success
                ? Results.Ok(response)
                : Results.Unauthorized();
        });
    }

    private static void MapCurrentUser(IEndpointRouteBuilder app)
    {
        app.MapGet("/auth/me", (HttpContext context) =>
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return Results.Ok(new
            {
                userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value,
                username = user.FindFirst(ClaimTypes.Name)?.Value ?? user.FindFirst("unique_name")?.Value,
                roleId = user.FindFirst("role_id")?.Value,
                storeId = user.FindFirst("store_id")?.Value,
                firstName = user.FindFirst("first_name")?.Value,
                lastName = user.FindFirst("last_name")?.Value
            });
        }).RequireAuthorization();
    }

    private static void MapChangePassword(IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/change-password", async (
            ICommandHandler<ChangePasswordCommand, ChangePasswordResponse> handler,
            HttpContext context,
            ChangePasswordRequest request,
            CancellationToken cancellationToken) =>
        {
            var idValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? context.User.FindFirst("sub")?.Value;

            if (!Guid.TryParse(idValue, out var userId))
            {
                return Results.Unauthorized();
            }

            var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
            var response = await handler.HandleAsync(command, cancellationToken);

            return response.Success
                ? Results.Ok(response)
                : Results.BadRequest(new { error = response.ErrorMessage });
        }).RequireAuthorization();
    }
}
