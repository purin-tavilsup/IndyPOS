using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Constants;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>
/// Seeds development test data for StoreHub.
/// Creates test users, products, and settings for manual testing.
/// Only runs in Development environment.
/// </summary>
public class DevelopmentDataSeeder
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreSettingRepository _settingRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        IStoreUserRepository userRepository,
        IProductRepository productRepository,
        IStoreSettingRepository settingRepository,
        IPasswordHasher passwordHasher,
        IStoreIdentityService storeIdentity,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _settingRepository = settingRepository;
        _passwordHasher = passwordHasher;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    /// <summary>
    /// Seeds test users, products, and settings if they don't exist.
    /// Safe to run multiple times.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Seeding development data...");

        await SeedSettingsAsync(cancellationToken);
        await SeedUsersAsync(cancellationToken);
        await SeedProductsAsync(cancellationToken);

        _logger.LogInformation("Development data seeding complete.");
    }

    private async Task SeedSettingsAsync(CancellationToken cancellationToken)
    {
        // Seed barcode counter if not exists
        var existingCounter = await _settingRepository.GetValueAsync(StoreSettingKeys.BarcodeCounter, cancellationToken);
        if (existingCounter is null)
        {
            await _settingRepository.SetValueAsync(StoreSettingKeys.BarcodeCounter, "0", cancellationToken);
            _logger.LogInformation("Initialized BarcodeCounter to 0");
        }
        else
        {
            _logger.LogDebug("BarcodeCounter already exists: {Value}", existingCounter);
        }
    }

    private async Task SeedUsersAsync(CancellationToken cancellationToken)
    {
        var storeId = _storeIdentity.StoreId;
        // RoleIds must match UserRole enum: Cashier=1, StoreManager=2, SystemAdmin=3
        var testUsers = new[]
        {
            new { Username = "admin", Password = "admin123", FirstName = "Admin", LastName = "User", RoleId = (int)UserRole.SystemAdmin },
            new { Username = "manager", Password = "manager123", FirstName = "Store", LastName = "Manager", RoleId = (int)UserRole.StoreManager },
            new { Username = "cashier", Password = "cashier123", FirstName = "Test", LastName = "Cashier", RoleId = (int)UserRole.Cashier }
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
        var storeId = _storeIdentity.StoreId;
        var generalGoodsCategory = ProductCategoryCodes.Miscellaneous;
        var testProducts = new[]
        {
            new { Barcode = "8850000000001", Name = "น้ำดื่ม 600ml", UnitPrice = 7m, Category = generalGoodsCategory },
            new { Barcode = "8850000000002", Name = "โค้ก 325ml", UnitPrice = 15m, Category = generalGoodsCategory },
            new { Barcode = "8850000000003", Name = "มาม่าหมูสับ", UnitPrice = 6m, Category = generalGoodsCategory },
            new { Barcode = "8850000000004", Name = "ขนมปังปี๊บ", UnitPrice = 20m, Category = generalGoodsCategory },
            new { Barcode = "8850000000005", Name = "นมจืด 200ml", UnitPrice = 12m, Category = generalGoodsCategory }
        };

        foreach (var testProduct in testProducts)
        {
            var existing = await _productRepository.GetByBarcodeAsync(testProduct.Barcode, cancellationToken);
            if (existing is not null)
            {
                if (existing.Name != testProduct.Name ||
                    existing.Description != testProduct.Name ||
                    existing.Category != testProduct.Category ||
                    existing.UnitPrice != testProduct.UnitPrice)
                {
                    existing.Name = testProduct.Name;
                    existing.Description = testProduct.Name;
                    existing.Category = testProduct.Category;
                    existing.UnitPrice = testProduct.UnitPrice;

                    await _productRepository.UpdateAsync(existing, cancellationToken);
                    _logger.LogInformation("Updated test product: {Name} ({Barcode})", testProduct.Name, testProduct.Barcode);
                }
                else
                {
                    _logger.LogDebug("Product {Barcode} already exists, skipping", testProduct.Barcode);
                }

                continue;
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
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
