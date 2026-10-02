using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Respawn;
using Xunit;
using LocalTokenOptions = IndyPOS.Application.Common.Models.LocalTokenOptions;

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

    /// <summary>
    /// A legacy user id no other test user in this run has. <c>store_user.legacy_user_id</c> is
    /// unique and the shared test database is not reset between tests, so random ids collided once
    /// enough users built up (the birthday problem) — a counter never repeats.
    /// </summary>
    protected static int NextLegacyUserId() => Interlocked.Increment(ref _lastLegacyUserId);

    // Starts well clear of the small ids that seeders and migrated users carry.
    private static int _lastLegacyUserId = 100_000;

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

        // Seed the payment-method catalog for the test store (mirrors production's
        // SeedPaymentMethodsAsync). Without this, CompleteSaleCommandHandler's offerable-set
        // check rejects every payment — including Cash — because the catalog is empty.
        // Resolved from the same scope so it seeds under the test host's
        // TestStoreIdentityService (StoreId "test-store"); idempotent, safe per-test.
        var paymentMethodSeeder = scope.ServiceProvider.GetRequiredService<PaymentMethodSeeder>();
        await paymentMethodSeeder.SeedAsync();

        // Same reason, for product categories: the create/update handlers reject a category the
        // catalogue does not define, so an unseeded catalogue fails every product write.
        var productCategorySeeder = scope.ServiceProvider.GetRequiredService<ProductCategorySeeder>();
        await productCategorySeeder.SeedAsync();

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
            LegacyUserId = NextLegacyUserId(),
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
    /// Signs a token through <see cref="LocalTokenOptions"/> (bound the same way <c>Program.cs</c>
    /// binds it: <c>config.GetSection(LocalTokenOptions.SectionName).Get&lt;LocalTokenOptions&gt;()
    /// ?? new LocalTokenOptions()</c>) rather than raw config indexing. The test host only sets
    /// <c>LocalToken:SecretKey</c> via <c>UseSetting</c> — a raw <c>section["Issuer"]</c> /
    /// <c>section["Audience"]</c> read comes back null, and a token minted with a null issuer/audience
    /// fails JWT bearer authentication before authorization or any endpoint filter ever runs. Binding
    /// through the options class picks up its Issuer/Audience defaults instead, so the token actually
    /// authenticates and the claims below are what determine the outcome.
    /// </summary>
    private string BuildToken(IEnumerable<Claim> claims)
    {
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var options = config.GetSection(LocalTokenOptions.SectionName).Get<LocalTokenOptions>() ?? new LocalTokenOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SecretKey));
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// A token that AUTHENTICATES (the cashier role by default, which has <c>cash.manage</c>) but carries NO
    /// sub/NameIdentifier claim — the capability check passes, so <see cref="RequireUserIdFilter"/> is
    /// the only thing standing between it and success. For a write (which calls
    /// <c>ClaimsPrincipal.GetRequiredUserId()</c>) that means a 500 if the filter is missing; for a
    /// read that never touches the user id it would otherwise succeed.
    /// </summary>
    protected string TokenWithoutUserId(UserRole role = UserRole.Cashier) =>
        BuildToken([new Claim("role_id", ((int)role).ToString()), new Claim("store_id", "test-store")]);

    /// <summary>A fully-formed token (user id present) whose role simply lacks the <c>cash.manage</c> capability.</summary>
    protected string TokenWithRole(int roleId) =>
        BuildToken(
        [
            new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()),
            new Claim("role_id", roleId.ToString()),
            new Claim("store_id", "test-store")
        ]);

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
    /// Writes an invoice straight to the database — no lines, no payments — leaving
    /// <see cref="Invoice.InvoiceNumber"/> at 0 so the column default assigns it. For bills on
    /// another day, which the sale endpoint cannot create.
    /// </summary>
    protected async Task<Invoice> SeedInvoiceAsync(DateTime createdUtc, decimal totalAmount = 350m, Guid? userId = null)
    {
        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            StoreId = TestStoreIdentityService.TestStoreId,
            UserId = userId ?? Guid.NewGuid(),
            TotalAmount = totalAmount,
            CreatedUtc = createdUtc,
            LastModifiedUtc = createdUtc
        };

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();

        return invoice;
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
