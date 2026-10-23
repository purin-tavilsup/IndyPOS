using IndyPOS.Cloud.Auth.Data;
using IndyPOS.Cloud.Auth.OpenIddict;
using IndyPOS.Cloud.Auth.StoreClients;
using IndyPOS.Cloud.Auth.Tokens;
using IndyPOS.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

namespace IndyPOS.Cloud.Auth;

/// <summary>
/// IndyPOS.Cloud.Auth: issues every cloud token (spec 2026-10-21, rule 3). A named entry point rather than top-level
/// statements, so a test project that hosts both Auth and CloudApi has one global Program, not two.
/// </summary>
public sealed class AuthApp
{
    private AuthApp()
    {
    }

    public static async Task Main(string[] args)
    {
        var app = Build(args);

        if (app.Environment.IsDevelopment())
        {
            await MigrateAsync(app);
        }
        else if (Array.Exists(args, a => string.Equals(a, "migrate", StringComparison.OrdinalIgnoreCase)))
        {
            // Production applies migrations only when run as "migrate", as CloudApi does.
            await MigrateAsync(app);
            return;
        }

        await app.RunAsync();
    }

    public static WebApplication Build(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Aspire's own database check is off: readiness is the one bounded check (API conventions).
        builder.AddNpgsqlDbContext<AuthDbContext>("auth-db", settings => settings.DisableHealthChecks = true);
        builder.AddDatabaseReadinessCheck("auth-db");

        // Checked here so a missing secret stops the start with its own message.
        ServiceClients.CloudApiSecret(builder.Configuration);

        builder.Services.AddAuthServer(builder.Configuration, builder.Environment);
        builder.Services.AddAuthentication();
        builder.Services.AddAuthorization(options => options.AddStoreClientPolicy());
        builder.Services.AddScoped<StoreClientService>();
        builder.Services.AddHostedService<ServiceClientSeeder>();

        var app = builder.Build();
        app.UseAuthForwardedHeaders();
        app.UseRequestTimeouts();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapDefaultEndpoints();
        app.MapTokenEndpoint();
        app.MapStoreClients();

        return app;
    }

    public static async Task MigrateAsync(WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Database.MigrateAsync();
    }
}
