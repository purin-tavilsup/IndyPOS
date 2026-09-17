namespace IndyPOS.CloudApi.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public static class CloudDbContextExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations. This is the production path for provisioning the schema;
    /// development uses EnsureCreated for speed. Kept off the normal start path so the compose
    /// one-shot either succeeds or fails before the API is allowed to start.
    /// </summary>
    public static async Task MigrateCloudDatabaseAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudDbContext>();
        await db.Database.MigrateAsync();
    }
}
