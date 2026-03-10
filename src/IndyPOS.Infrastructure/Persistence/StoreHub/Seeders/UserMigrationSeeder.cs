using Dapper;
using IndyPOS.Application.Abstractions.Pos.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Domain.Entities.Core;
using Microsoft.Extensions.Logging;

namespace IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;

/// <summary>
/// Migrates users from legacy SQLite database to StoreHub PostgreSQL.
/// Passwords are kept as-is with PasswordHashVersion=1 (TripleDES).
/// BCrypt upgrade happens on first login via StoreAuthService.
/// </summary>
public class UserMigrationSeeder
{
    private readonly IStoreUserRepository _storeUserRepository;
    private readonly IDbConnectionProvider _sqliteConnectionProvider;
    private readonly IStoreIdentityService _storeIdentity;
    private readonly ILogger<UserMigrationSeeder> _logger;

    public UserMigrationSeeder(
        IStoreUserRepository storeUserRepository,
        IDbConnectionProvider sqliteConnectionProvider,
        IStoreIdentityService storeIdentity,
        ILogger<UserMigrationSeeder> logger)
    {
        _storeUserRepository = storeUserRepository;
        _sqliteConnectionProvider = sqliteConnectionProvider;
        _storeIdentity = storeIdentity;
        _logger = logger;
    }

    /// <summary>
    /// Migrates all users from SQLite to PostgreSQL.
    /// Safe to run multiple times - uses UPSERT logic via LegacyUserId.
    /// </summary>
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting user migration from SQLite to PostgreSQL...");

        var legacyUsers = await GetLegacyUsersAsync();
        var storeId = _storeIdentity.StoreId;
        var migratedCount = 0;
        var skippedCount = 0;

        foreach (var (user, credential) in legacyUsers)
        {
            try
            {
                // Check if already migrated (UPSERT logic)
                var existing = await _storeUserRepository.GetByLegacyIdAsync(user.UserId, cancellationToken);
                if (existing is not null)
                {
                    _logger.LogDebug("User {Username} (LegacyId={LegacyId}) already migrated, skipping",
                        credential.Username, user.UserId);
                    skippedCount++;
                    continue;
                }

                // Create new StoreUser with legacy password (version 1)
                var storeUser = new StoreUser
                {
                    Id = Guid.NewGuid(),
                    StoreId = storeId,
                    LegacyUserId = user.UserId,
                    Username = credential.Username,
                    PasswordHash = credential.Password, // Keep TripleDES encrypted value
                    PasswordHashVersion = 1, // Mark as legacy (will upgrade on first login)
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RoleId = user.RoleId,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    LastModifiedAtUtc = DateTime.UtcNow,
                    LastLoginAtUtc = null,
                    CloudUserId = null
                };

                await _storeUserRepository.AddAsync(storeUser, cancellationToken);
                migratedCount++;

                _logger.LogInformation("Migrated user: {Username} (LegacyId={LegacyId})",
                    credential.Username, user.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate user: {Username} (LegacyId={LegacyId})",
                    credential.Username, user.UserId);
            }
        }

        _logger.LogInformation(
            "User migration complete. Migrated: {Migrated}, Skipped: {Skipped}, Total: {Total}",
            migratedCount, skippedCount, legacyUsers.Count);
    }

    private async Task<List<(Domain.Entities.UserAccount User, Domain.Entities.UserCredential Credential)>> GetLegacyUsersAsync()
    {
        // Note: SQLite's IDbConnection doesn't support OpenAsync, but Dapper's QueryAsync handles it
        using var connection = _sqliteConnectionProvider.GetDbConnection();
        connection.Open(); // Sync open is fine for SQLite local file access

        const string sql = """
            SELECT
                u.UserId, u.FirstName, u.LastName, u.RoleId, u.DateCreated, u.DateUpdated,
                c.UserId, c.Username, c.Password, c.DateCreated, c.DateUpdated
            FROM User u
            INNER JOIN UserCredential c ON u.UserId = c.UserId
            """;

        var results = await connection.QueryAsync<
            Domain.Entities.UserAccount,
            Domain.Entities.UserCredential,
            (Domain.Entities.UserAccount, Domain.Entities.UserCredential)>(
            sql,
            (user, cred) => (user, cred),
            splitOn: "UserId");

        return results.ToList();
    }
}
