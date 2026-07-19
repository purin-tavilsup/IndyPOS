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
    /// Returns true if seeding occurred; false if admin already existed.
    /// </summary>
    public static async Task<bool> SeedInitialAdminAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<InitialAdminSeeder>();
        return await seeder.SeedAsync();
    }

    /// <summary>
    /// Seeds the known payment methods for this store (Cash, MoneyTransfer,
    /// WelfareCard, PayLater, and dead government campaigns disabled).
    /// Idempotent — safe to run on every start.
    /// </summary>
    public static async Task SeedPaymentMethodsAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<PaymentMethodSeeder>();
        await seeder.SeedAsync();
    }

    /// <summary>
    /// Recovery entry point for the "reset-admin" CLI: generates a fresh random
    /// password, (re)sets the admin with must-change, and returns the password
    /// so the caller can print it once.
    /// </summary>
    public static async Task<string> ResetAdminAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<InitialAdminSeeder>();
        var password = IndyPOS.Infrastructure.Services.StoreHub.AdminPasswordGenerator.Generate();
        await seeder.ResetAsync(password);
        return password;
    }
}
