using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IndyPOS.Infrastructure.Persistence.StoreHub;

public static class StoreHubDbContextExtensions
{
    /// <summary>
    /// Ensures the StoreHub database schema is created.
    /// Only use in development - production should use migrations.
    /// </summary>
    public static async Task EnsureStoreHubDatabaseCreatedAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    /// <summary>
    /// Seeds development test data (users and products).
    /// Safe to run multiple times - uses idempotent UPSERT logic.
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>();
        await seeder.SeedAsync();
    }

    /// <summary>
    /// Applies pending EF Core migrations. This is the production path for
    /// provisioning the schema (dev uses EnsureCreated for speed).
    /// </summary>
    public static async Task MigrateStoreHubDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Seeds the initial admin login from the InitialAdmin configuration
    /// section. Idempotent — safe to run on every start.
    /// </summary>
    public static async Task SeedInitialAdminAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<InitialAdminSeeder>();
        await seeder.SeedAsync();
    }
}
