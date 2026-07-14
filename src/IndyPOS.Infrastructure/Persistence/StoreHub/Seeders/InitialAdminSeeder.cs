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

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        var username = _configuration["InitialAdmin:Username"];
        var password = _configuration["InitialAdmin:Password"];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogInformation("No InitialAdmin configured; skipping admin seed.");
            return false;
        }

        var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing is not null)
        {
            _logger.LogInformation("Initial admin '{Username}' already exists; skipping seed.", username);
            return false;
        }

        await _userRepository.AddAsync(BuildAdmin(username, password), cancellationToken);
        _logger.LogInformation("Seeded initial admin user '{Username}' (SystemAdmin, must-change=true).", username);
        return true;
    }

    /// <summary>
    /// Recovery path: (re)sets the admin password to <paramref name="newPassword"/>
    /// and re-arms must-change. Creates the admin if absent. Used by the
    /// "reset-admin" CLI when the finish-screen credential is lost.
    /// </summary>
    public async Task ResetAsync(string newPassword, CancellationToken cancellationToken = default)
    {
        var username = _configuration["InitialAdmin:Username"];
        if (string.IsNullOrWhiteSpace(username))
        {
            username = "admin";
        }

        var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing is null)
        {
            await _userRepository.AddAsync(BuildAdmin(username, newPassword), cancellationToken);
        }
        else
        {
            await _userRepository.SetPasswordAsync(existing.Id, _passwordHasher.Hash(newPassword), mustChangePassword: true, cancellationToken);
        }

        _logger.LogInformation("Reset admin '{Username}' (must-change=true).", username);
    }

    private StoreUser BuildAdmin(string username, string password) => new()
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
        MustChangePassword = true,
        CreatedAtUtc = DateTime.UtcNow,
        LastModifiedAtUtc = DateTime.UtcNow
    };
}
