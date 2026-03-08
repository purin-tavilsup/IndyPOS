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
}
