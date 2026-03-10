using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Local authentication service for StoreHub.
/// Handles BCrypt verification and legacy TripleDES migration.
/// </summary>
public class StoreAuthService : IStoreAuthService
{
    private readonly IStoreUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICryptographyService _legacyCrypto;
    private readonly ILocalTokenService _tokenService;
    private readonly ILogger<StoreAuthService> _logger;

    public StoreAuthService(
        IStoreUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ICryptographyService legacyCrypto,
        ILocalTokenService tokenService,
        ILogger<StoreAuthService> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _legacyCrypto = legacyCrypto;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failed("Username and password are required");
        }

        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("Login attempt for non-existent user: {Username}", username);
            return AuthResult.Failed("Invalid credentials");
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {Username}", username);
            return AuthResult.Failed("Account is inactive");
        }

        bool isValid;

        if (user.PasswordHashVersion == 1)
        {
            // Legacy TripleDES - verify and migrate to BCrypt
            isValid = VerifyLegacyPassword(password, user.PasswordHash);

            if (isValid)
            {
                // Upgrade to BCrypt
                var bcryptHash = _passwordHasher.Hash(password);
                await _userRepository.UpdatePasswordHashAsync(user.Id, bcryptHash, version: 2, cancellationToken);
                _logger.LogInformation("Migrated user {Username} from TripleDES to BCrypt", username);
            }
        }
        else
        {
            // BCrypt verification
            isValid = _passwordHasher.Verify(password, user.PasswordHash);
        }

        if (!isValid)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", username);
            return AuthResult.Failed("Invalid credentials");
        }

        // Update last login
        await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, cancellationToken);

        // Generate token
        var token = _tokenService.GenerateToken(user);

        _logger.LogInformation("User {Username} logged in successfully", username);

        return AuthResult.Succeeded(token, new AuthenticatedUser(
            Id: user.Id,
            Username: user.Username,
            FirstName: user.FirstName,
            LastName: user.LastName,
            RoleId: user.RoleId,
            StoreId: user.StoreId));
    }

    private bool VerifyLegacyPassword(string plainPassword, string storedHash)
    {
        try
        {
            // Legacy system stored encrypted password, not hashed
            // Encrypt the input and compare with stored value
            var encryptedInput = _legacyCrypto.Encrypt(plainPassword);
            return encryptedInput == storedHash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying legacy password");
            return false;
        }
    }
}
