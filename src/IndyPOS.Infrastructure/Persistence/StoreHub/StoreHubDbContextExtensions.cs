using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace IndyPOS.Infrastructure.Persistence.StoreHub;

public static class StoreHubDbContextExtensions
{
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
    /// provisioning the schema; Development applies them too, on start.
    /// </summary>
    public static async Task MigrateStoreHubDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// The Development start: refuses a database the old EnsureCreated path built, then migrates. Production
    /// never runs this; its databases were always migrated, and dropping one is never the answer there.
    /// </summary>
    public static async Task MigrateDevelopmentStoreHubDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        await RejectEnsureCreatedDatabaseAsync(db);
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Throws when the database has tables but no migration history: EnsureCreated built it, so migrating
    /// would replay the first migration and fail on a table that already exists.
    /// </summary>
    public static async Task RejectEnsureCreatedDatabaseAsync(DbContext db, CancellationToken cancellationToken = default)
    {
        var creator = db.GetService<IRelationalDatabaseCreator>();
        if (!await creator.ExistsAsync(cancellationToken) || !await creator.HasTablesAsync(cancellationToken))
            return;

        if ((await db.Database.GetAppliedMigrationsAsync(cancellationToken)).Any())
            return;

        throw new InvalidOperationException(
            $"Database '{db.Database.GetDbConnection().Database}' was built by EnsureCreated, which StoreHub no longer " +
            "uses, so it cannot be migrated. Point StoreHub at a new database, or drop this one (its dev data is lost).");
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
    /// Seeds this store's product categories, chosen by store type.
    /// Idempotent — safe to run on every start.
    /// </summary>
    public static async Task SeedProductCategoriesAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ProductCategorySeeder>();
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
