using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests;

/// <summary>
/// Base class for StoreHub integration tests.
/// Provides common utilities for database seeding, authentication, and HTTP requests.
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<StoreHubWebApplicationFactory>, IAsyncLifetime
{
    protected readonly StoreHubWebApplicationFactory Factory;
    protected readonly HttpClient Client;
    private Respawner? _respawner;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    protected IntegrationTestBase(StoreHubWebApplicationFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Ensure database schema is created
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        await db.Database.EnsureCreatedAsync();

        // Initialize Respawner for database cleanup between tests
        // For PostgreSQL, we need to pass an open connection, not a connection string
        await using var connection = new NpgsqlConnection(Factory.ConnectionString);
        await connection.OpenAsync();

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"]
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Resets the database to a clean state.
    /// Call this between tests if needed.
    /// </summary>
    protected async Task ResetDatabaseAsync()
    {
        if (_respawner is not null)
        {
            await using var connection = new NpgsqlConnection(Factory.ConnectionString);
            await connection.OpenAsync();
            await _respawner.ResetAsync(connection);
        }
    }

    /// <summary>
    /// Creates a test user and returns the user entity.
    /// </summary>
    protected async Task<StoreUser> CreateTestUserAsync(
        string username = "testuser",
        string password = "Password123!",
        UserRole role = UserRole.Cashier,
        bool isActive = true)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var user = new StoreUser
        {
            Id = Guid.NewGuid(),
            StoreId = "test-store",
            LegacyUserId = Random.Shared.Next(1000, 9999),
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            PasswordHashVersion = 2, // BCrypt
            FirstName = "Test",
            LastName = "User",
            RoleId = (int)role,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        };

        db.StoreUsers.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Authenticates as a test user and sets the authorization header.
    /// Returns the JWT token.
    /// </summary>
    protected async Task<string> AuthenticateAsAsync(
        string username = "testuser",
        string password = "Password123!",
        UserRole role = UserRole.Cashier)
    {
        // Create user if not exists
        await CreateTestUserAsync(username, password, role);

        // Login to get token
        var loginResponse = await Client.PostAsJsonAsync("/auth/login", new
        {
            username,
            password
        });

        loginResponse.EnsureSuccessStatusCode();

        var result = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        var token = result?.Token ?? throw new InvalidOperationException("Login failed");

        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return token;
    }

    /// <summary>
    /// Authenticates as a cashier (basic permissions).
    /// Uses unique username per call to avoid conflicts.
    /// </summary>
    protected Task<string> AuthenticateAsCashierAsync()
    {
        var username = $"cashier_{Guid.NewGuid():N}";
        return AuthenticateAsAsync(username, "Cashier123!", UserRole.Cashier);
    }

    /// <summary>
    /// Authenticates as a store manager (elevated permissions).
    /// Uses unique username per call to avoid conflicts.
    /// </summary>
    protected Task<string> AuthenticateAsManagerAsync()
    {
        var username = $"manager_{Guid.NewGuid():N}";
        return AuthenticateAsAsync(username, "Manager123!", UserRole.StoreManager);
    }

    /// <summary>
    /// Authenticates as a system admin (full permissions).
    /// Uses unique username per call to avoid conflicts.
    /// </summary>
    protected Task<string> AuthenticateAsAdminAsync()
    {
        var username = $"admin_{Guid.NewGuid():N}";
        return AuthenticateAsAsync(username, "Admin123!", UserRole.SystemAdmin);
    }

    /// <summary>
    /// Clears the authorization header.
    /// </summary>
    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }

    /// <summary>
    /// Creates a test product and returns the product entity.
    /// Also creates an initial inventory movement to set the stock level.
    /// </summary>
    protected async Task<Product> CreateTestProductAsync(
        string? barcode = null,
        string name = "Test Product",
        decimal unitPrice = 100m,
        int initialStock = 10,
        bool isActive = true)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var product = new Product
        {
            Id = Guid.NewGuid(),
            StoreId = "test-store",
            Barcode = barcode ?? $"TEST{Random.Shared.Next(100000, 999999)}",
            Name = name,
            Description = name,
            Category = "General",
            UnitPrice = unitPrice,
            IsActive = isActive,
            CreatedUtc = DateTime.UtcNow,
            LastModifiedUtc = DateTime.UtcNow
        };

        db.Products.Add(product);

        // Add initial stock via inventory movement
        if (initialStock > 0)
        {
            var movement = new InventoryMovement
            {
                Id = Guid.NewGuid(),
                StoreId = "test-store",
                ProductId = product.Id,
                QuantityDelta = initialStock,
                Reason = "InitialStock",
                CreatedUtc = DateTime.UtcNow
            };
            db.InventoryMovements.Add(movement);
        }

        await db.SaveChangesAsync();

        return product;
    }

    /// <summary>
    /// Gets the current stock for a product by summing inventory movements.
    /// </summary>
    protected async Task<int> GetProductStockAsync(Guid productId)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        return db.InventoryMovements
            .Where(m => m.ProductId == productId)
            .Sum(m => m.QuantityDelta);
    }

    /// <summary>
    /// Gets the database context for direct database operations.
    /// </summary>
    protected StoreHubDbContext GetDbContext()
    {
        var scope = Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
    }
}
