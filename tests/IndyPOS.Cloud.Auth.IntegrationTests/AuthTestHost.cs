using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IndyPOS.Cloud.Auth.IntegrationTests;

/// <summary>Auth's real entry point on a TestServer, against one test database. Settings override appsettings.</summary>
public class AuthTestHost(string connectionString, IReadOnlyDictionary<string, string?>? settings = null)
    : WebApplicationFactory<AuthApp>
{
    /// <summary>CloudApi's service-client secret in every test host, unless a test overrides it.</summary>
    public const string ServiceClientSecret = "cloud-api-test-secret";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:auth-db", connectionString);
        builder.UseSetting("ServiceClients:CloudApi:ClientSecret", ServiceClientSecret);

        foreach (var (key, value) in settings ?? new Dictionary<string, string?>())
            builder.UseSetting(key, value);
    }
}
