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
/// Creates test users, settings, and the running store type's profile products (with opening stock)
/// for manual testing.
/// Only runs in Development environment.
/// </summary>
public class DevelopmentDataSeeder
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStoreSettingRepository _settingRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly IInventoryMovementRepository _inventoryMovements;
    private readonly ILogger<DevelopmentDataSeeder> _logger;

    public DevelopmentDataSeeder(
        IStoreUserRepository userRepository,
        IProductRepository productRepository,
        IStoreSettingRepository settingRepository,
        IPasswordHasher passwordHasher,
        IStoreIdentityService storeIdentity,
        IInventoryMovementRepository inventoryMovements,
        ILogger<DevelopmentDataSeeder> logger)
    {
        _userRepository = userRepository;
        _productRepository = productRepository;
        _settingRepository = settingRepository;
        _passwordHasher = passwordHasher;
        _storeIdentity = storeIdentity;
        _inventoryMovements = inventoryMovements;
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
        var profile = IndyPOS.StoreProfiles.StoreProfiles.ForType(_storeIdentity.StoreType);

        foreach (var seed in profile.Products)
        {
            var existing = await _productRepository.GetByBarcodeAsync(seed.Barcode, cancellationToken);
            if (existing is not null)
            {
                await RefreshAsync(existing, seed, cancellationToken);
                continue;
            }

            var product = new Product
            {
                Id = Guid.NewGuid(),
                StoreId = storeId,
                Barcode = seed.Barcode,
                Name = seed.Name,
                Description = seed.Name,
                Category = seed.Category,
                UnitPrice = seed.UnitPrice,
                IsTrackable = seed.IsTrackable,
                IsActive = true,
                CreatedUtc = DateTime.UtcNow,
                LastModifiedUtc = DateTime.UtcNow
            };
            await _productRepository.AddAsync(product, cancellationToken);
            await AddOpeningStockAsync(product, seed, cancellationToken);
            _logger.LogInformation("Created dev product for {Store}: {Name} ({Barcode})", profile.Key, seed.Name, seed.Barcode);
        }
    }

    // Opening stock only when the product is first created, so a re-seed never doubles it.
    private async Task AddOpeningStockAsync(Product product, IndyPOS.StoreProfiles.StoreProfileProduct seed,
                                            CancellationToken cancellationToken)
    {
        if (!seed.IsTrackable || seed.InitialStock <= 0)
            return;

        await _inventoryMovements.AddAsync(new InventoryMovement
        {
            Id = Guid.NewGuid(),
            StoreId = product.StoreId,
            ProductId = product.Id,
            QuantityDelta = seed.InitialStock,
            Reason = "InitialStock",
            CreatedUtc = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task RefreshAsync(Product existing, IndyPOS.StoreProfiles.StoreProfileProduct seed,
                                    CancellationToken cancellationToken)
    {
        if (existing.Name == seed.Name && existing.Category == seed.Category &&
            existing.UnitPrice == seed.UnitPrice && existing.IsTrackable == seed.IsTrackable)
            return;

        existing.Name = seed.Name;
        existing.Description = seed.Name;
        existing.Category = seed.Category;
        existing.UnitPrice = seed.UnitPrice;
        existing.IsTrackable = seed.IsTrackable;
        await _productRepository.UpdateAsync(existing, cancellationToken);
    }
}
