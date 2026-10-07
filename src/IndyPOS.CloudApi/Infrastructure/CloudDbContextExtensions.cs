namespace IndyPOS.CloudApi.Infrastructure;

using Microsoft.EntityFrameworkCore;
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
}
