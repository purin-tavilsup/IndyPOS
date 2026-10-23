using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IndyPOS.Cloud.Auth.IntegrationTests;

/// <summary>Auth's real entry point on a TestServer, against one test database. Settings override appsettings.</summary>
public sealed class AuthTestHost(string connectionString, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<AuthApp>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:auth-db", connectionString);

        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            builder.UseSetting(key, value);
    }
}
