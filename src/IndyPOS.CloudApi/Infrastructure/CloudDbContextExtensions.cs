namespace IndyPOS.CloudApi.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class CloudDbContextExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations. Development runs it on every start; production runs it only
    /// through the "migrate" argument, kept off the normal start path so the compose one-shot either
    /// succeeds or fails before the API is allowed to start.
    /// </summary>
    public static async Task MigrateCloudDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// The Development start: refuses a database the old EnsureCreated path built, then migrates. Production
    /// never runs this; its database was always migrated, and dropping one is never the answer there.
    /// </summary>
    public static async Task MigrateDevelopmentCloudDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
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
            $"Database '{db.Database.GetDbConnection().Database}' was built by EnsureCreated, which CloudApi no longer " +
            "uses, so it cannot be migrated. Point CloudApi at a new database, or drop this one (its dev data is lost).");
    }
}
