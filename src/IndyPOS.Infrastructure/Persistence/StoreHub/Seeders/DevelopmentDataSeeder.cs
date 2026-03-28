using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>
/// Seeds development test data for StoreHub.
/// Creates test users and products for manual testing.
/// Only runs in Development environment.
/// </summary>
public class DevelopmentDataSeeder
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        IStoreUserRepository userRepository,
        IProductRepository productRepository,
        IPasswordHasher passwordHasher,
        IStoreIdentityService storeIdentity,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _passwordHasher = passwordHasher;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    /// <summary>
    /// Seeds test users and products if they don't exist.
    /// Safe to run multiple times.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding development data...");

        await SeedUsersAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);

        _logger.LogInformation("Development data seeding complete.");
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var storeId = _storeIdentity.StoreId;
        var testUsers = new[]
        {
            new { Username = "admin", Password = "admin123", FirstName = "Admin", LastName = "User", RoleId = 1 },
            new { Username = "manager", Password = "manager123", FirstName = "Store", LastName = "Manager", RoleId = 2 },
            new { Username = "cashier", Password = "cashier123", FirstName = "Test", LastName = "Cashier", RoleId = 3 }
        };

        foreach (var testUser in testUsers)
        {
            var existing = await _userRepository.GetByUsernameAsync(testUser.Username, cancellationToken);
            if (existing is not null)
            {
                _logger.LogDebug("User {Username} already exists, skipping", testUser.Username);
                continue;
            }

            var user = new StoreUser
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Username = testUser.Username,
                PasswordHash = _passwordHasher.Hash(testUser.Password),
                PasswordHashVersion = 2, // BCrypt
                FirstName = testUser.FirstName,
                LastName = testUser.LastName,
                RoleId = testUser.RoleId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                LastModifiedAtUtc = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user, cancellationToken);
            _logger.LogInformation("Created test user: {Username} (Role: {RoleId})", testUser.Username, testUser.RoleId);
        }
    }

    private async Task SeedProductsAsync(CancellationToken cancellationToken)
    {
        var testProducts = new[]
        {
            new { Barcode = "8850000000001", Name = "น้ำดื่ม 600ml", UnitPrice = 7m, Category = "เครื่องดื่ม" },
            new { Barcode = "8850000000002", Name = "โค้ก 325ml", UnitPrice = 15m, Category = "เครื่องดื่ม" },
            new { Barcode = "8850000000003", Name = "มาม่าหมูสับ", UnitPrice = 6m, Category = "อาหาร" },
            new { Barcode = "8850000000004", Name = "ขนมปังปี๊บ", UnitPrice = 20m, Category = "ขนม" },
            new { Barcode = "8850000000005", Name = "นมจืด 200ml", UnitPrice = 12m, Category = "เครื่องดื่ม" }
        };

        foreach (var testProduct in testProducts)
        {
            var existing = await _productRepository.GetByBarcodeAsync(testProduct.Barcode, cancellationToken);
            if (existing is not null)
            {
                _logger.LogDebug("Product {Barcode} already exists, skipping", testProduct.Barcode);
                continue;
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Barcode = testProduct.Barcode,
                Name = testProduct.Name,
                Description = testProduct.Name,
                Category = testProduct.Category,
                UnitPrice = testProduct.UnitPrice,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow,
                LastModifiedUtc = DateTime.UtcNow
            };

            await _productRepository.AddAsync(product, cancellationToken);
            _logger.LogInformation("Created test product: {Name} ({Barcode})", testProduct.Name, testProduct.Barcode);
        }
    }
}
