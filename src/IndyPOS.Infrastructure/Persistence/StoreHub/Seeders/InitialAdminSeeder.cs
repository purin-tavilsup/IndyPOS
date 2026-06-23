using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>
/// Seeds the initial SystemAdmin login on a fresh production install.
/// Credentials come from the installer wizard via the <c>InitialAdmin</c>
/// configuration section. Idempotent: skips if the admin username already
/// exists, so it is safe to run on every service start.
/// </summary>
public class InitialAdminSeeder
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InitialAdminSeeder> _logger;

    public InitialAdminSeeder(
        IStoreUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IStoreIdentityService storeIdentity,
        IConfiguration configuration,
        ILogger<InitialAdminSeeder> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _storeIdentity = storeIdentity;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var username = _configuration["InitialAdmin:Username"];
        var password = _configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            // No initial admin configured (e.g. an upgrade over an existing DB
            // that already has users). Nothing to do.
            _logger.LogInformation("No InitialAdmin configured; skipping admin seed.");
            return;
        }

        var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Initial admin '{Username}' already exists; skipping seed.", username);
            return;
        }

        var admin = new StoreUser
        {
            Id = Guid.NewGuid(),
            StoreId = _storeIdentity.StoreId,
            Username = username,
            PasswordHash = _passwordHasher.Hash(password),
            PasswordHashVersion = 2, // BCrypt
            FirstName = "Store",
            LastName = "Administrator",
            RoleId = (int)UserRole.SystemAdmin,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedAtUtc = DateTime.UtcNow
        };

        await _userRepository.AddAsync(admin, cancellationToken);
        _logger.LogInformation("Seeded initial admin user '{Username}' (SystemAdmin).", username);
    }
}
