using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Enums;
using IndyPOS.Domain.ValueObjects;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// WebApplicationFactory for StoreHub integration tests using Testcontainers PostgreSQL.
/// Provides a real PostgreSQL database for realistic integration testing.
/// </summary>
public class StoreHubWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("storehub_test")
        .WithUsername("test_user")
        .WithPassword("test_password")
        .Build();

    public string ConnectionString => _postgresContainer.GetConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to Testing and provide connection string
        builder.UseSetting("ConnectionStrings:storehub-db", _postgresContainer.GetConnectionString());
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove ALL registrations related to StoreHubDbContext
            // This includes the pooled context factory and the context itself
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType == typeof(StoreHubDbContext) ||
                    d.ServiceType == typeof(DbContextOptions<StoreHubDbContext>) ||
                    d.ServiceType.FullName?.Contains("StoreHubDbContext") == true ||
                    d.ServiceType.FullName?.Contains("DbContextPool") == true)
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
            {
                services.Remove(descriptor);
            }

            // Add fresh DbContext WITHOUT pooling to avoid complexity
            services.AddDbContext<StoreHubDbContext>(options =>
            {
                options.UseNpgsql(_postgresContainer.GetConnectionString());
            }, ServiceLifetime.Scoped, ServiceLifetime.Scoped);

            // Replace IStoreIdentityService with test implementation
            services.RemoveAll<IStoreIdentityService>();
            services.AddSingleton<IStoreIdentityService>(new TestStoreIdentityService());
        });
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await base.DisposeAsync();
    }
}

/// <summary>
/// Test implementation of IStoreIdentityService with known values.
/// </summary>
internal class TestStoreIdentityService : IStoreIdentityService
{
    public const string TestStoreId = "test-store";

    public string StoreId => TestStoreId;
    public string StoreName => "Test Store";
    public StoreType StoreType => StoreType.GeneralHardware;
    public StoreTypeFeatures Features => StoreTypeFeatures.For(StoreType.GeneralHardware);
    public TimeZoneInfo TimeZone => TimeZoneInfo.Local;

    [Obsolete("Use StoreId (UUID) for identification.")]
    public int StoreCode => 1;

    public void EnsureConfigured() { }
}
