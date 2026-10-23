using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IndyPOS.Cloud.Auth.Data;

/// <summary>
/// Lets `dotnet ef` build the model without starting the app. The fallback connection string is never connected
/// to; `dotnet ef database update` overrides it with ConnectionStrings__auth-db, as CloudApi's factory does.
/// </summary>
public sealed class AuthDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__auth-db")
            ?? "Host=design-time-only;Database=indypos_auth;Username=none;Password=none";

        return new AuthDbContext(new DbContextOptionsBuilder<AuthDbContext>().UseNpgsql(connectionString).Options);
    }
}
