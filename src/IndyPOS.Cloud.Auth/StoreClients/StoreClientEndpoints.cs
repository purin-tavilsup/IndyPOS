using IndyPOS.Application.Common.Constants;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;

namespace IndyPOS.Cloud.Auth.StoreClients;

/// <summary>
/// The store-client API: CloudApi registers, deactivates and (to undo a failed registration) deletes a store's
/// client. Only a token with the auth.store-clients scope may call it, which only the cloud-api service client can get.
/// </summary>
internal static class StoreClientEndpoints
{
    public const string Policy = "StoreClients";

    public sealed record CreateStoreClientBody(string DisplayName);

    public static void AddStoreClientPolicy(this Microsoft.AspNetCore.Authorization.AuthorizationOptions options) =>
        options.AddPolicy(Policy, policy =>
        {
            policy.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => context.User.HasScope(AuthScopes.StoreClients));
        });

    public static IEndpointRouteBuilder MapStoreClients(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/store-clients/{storeId}").RequireAuthorization(Policy);

        group.MapPut("", async (string storeId, CreateStoreClientBody body, StoreClientService service, CancellationToken ct) =>
            await service.CreateOrReplaceAsync(storeId, body.DisplayName, ct) is { } credentials
                ? Results.Ok(credentials)
                : Results.Conflict(new { error = $"Store '{storeId}' was deactivated. A store that comes back is registered as a new store." }));

        group.MapPost("/disable", async (string storeId, StoreClientService service, CancellationToken ct) =>
        {
            await service.DisableAsync(storeId, ct);
            return Results.NoContent();
        });

        group.MapDelete("", async (string storeId, StoreClientService service, CancellationToken ct) =>
        {
            await service.DeleteAsync(storeId, ct);
            return Results.NoContent();
        });

        return app;
    }
}
