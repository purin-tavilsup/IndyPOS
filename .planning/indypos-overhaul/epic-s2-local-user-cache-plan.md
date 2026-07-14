# Epic S2: Local User Cache - Implementation Plan

**Status:** ✅ COMPLETE
**Completed:** 2026-03-10
**Tests:** 8 new tests (94 total passing)

## Overview

Sync users from CloudApi (master) to StoreHub (local cache) for offline authentication.

**Source of Truth:** CloudApi owns user lifecycle (create, deactivate, rename, role assignment)
**Local Replica:** StoreHub keeps synced projection for offline auth

---

## Architecture

```
CloudApi (Master)                    StoreHub (Local Cache)
================                     =====================

[CloudUser]                          [StoreUser]
   │                                    ↑
   │ GET /master/users/{storeId}        │
   │ ?modifiedSince=...&sinceVersion=N  │
   └──────────────────────────────────→ │
                                   UserSyncService
                                   (Background pull + retry)
                                        │
                                   StoreUserRepository
                                   .UpsertByCloudIdAsync()
```

**Runtime Behavior:**
- **Online:** Cloud changes sync down to StoreHub
- **Offline:** POS authenticates against local StoreUser table
- **Reconnected:** StoreHub resumes sync from CloudApi (with retry)

---

## Implementation Phases

### Phase 1: CloudApi - CloudUser Entity

| File | Action | Description |
|------|--------|-------------|
| `CloudApi/Domain/CloudUser.cs` | Create | Master user entity |
| `CloudApi/Infrastructure/CloudDbContext.cs` | Modify | Add DbSet<CloudUser> |
| `CloudApi/Infrastructure/Configurations/CloudUserConfiguration.cs` | Create | EF config |

**CloudUser Entity:**
```csharp
public class CloudUser
{
    public Guid Id { get; set; }
    public string StoreId { get; set; } = default!;
    public string Username { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
    public long Version { get; set; }  // Monotonic version for sync safety
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
}
```

**Note:** No PasswordHash - passwords are managed locally at each store.
**Note:** `Version` is auto-incremented on every update to prevent clock drift issues.

---

### Phase 2: CloudApi - Master Users Endpoint

| File | Action | Description |
|------|--------|-------------|
| `CloudApi/Program.cs` | Modify | Add GET /master/users/{storeId} |

**Endpoint:**
```csharp
// GET /master/users/{storeId}?sinceVersion=5
app.MapGet("/master/users/{storeId}", async (
    string storeId,
    long? sinceVersion,
    CloudDbContext db,
    ClaimsPrincipal user) =>
{
    // Authorization: Ensure requesting store can only fetch its own users
    var tokenStoreId = user.FindFirst("store_id")?.Value;
    if (tokenStoreId != storeId)
        return Results.Forbid();

    var query = db.Users.Where(u => u.StoreId == storeId);

    if (sinceVersion.HasValue)
        query = query.Where(u => u.Version > sinceVersion.Value);

    var users = await query.OrderBy(u => u.Version).ToListAsync();
    var maxVersion = users.Any() ? users.Max(u => u.Version) : sinceVersion ?? 0;

    return Results.Ok(new CloudUserSyncResponse(users.Count, users, maxVersion, DateTime.UtcNow));
}).RequireAuthorization();
```

**Response DTO:**
```csharp
public record CloudUserSyncResponse(
    int Count,
    List<CloudUser> Users,
    long MaxVersion,
    DateTime Timestamp);
```

---

### Phase 3: StoreHub - Sync Infrastructure

| File | Action | Description |
|------|--------|-------------|
| `Domain/Entities/Core/StoreUser.cs` | Modify | Add LastSyncedAtUtc |
| `Application/Abstractions/StoreHub/Services/IUserSyncService.cs` | Create | Sync interface |
| `Application/Abstractions/StoreHub/Repositories/IStoreUserRepository.cs` | Modify | Add UpsertByCloudIdAsync |
| `Infrastructure/Persistence/StoreHub/Repositories/StoreUserRepository.cs` | Modify | Implement upsert |

**StoreUser additions:**
```csharp
public DateTime? LastSyncedAtUtc { get; set; }  // Track sync status
public long CloudVersion { get; set; }          // Version from cloud for sync safety
```

**IUserSyncService:**
```csharp
public interface IUserSyncService
{
    Task<int> SyncUsersFromCloudAsync(CancellationToken ct = default);
}
```

**Repository UpsertByCloudIdAsync:**
```csharp
Task UpsertByCloudIdAsync(StoreUser user, CancellationToken ct = default);
// If CloudUserId exists → update
// If CloudUserId not found → insert new
```

---

### Phase 4: StoreHub - HttpCloudSyncClient Extension

| File | Action | Description |
|------|--------|-------------|
| `Application/Abstractions/StoreHub/Services/ICloudSyncClient.cs` | Modify | Add GetUsersAsync |
| `Infrastructure/Services/StoreHub/HttpCloudSyncClient.cs` | Modify | Implement GetUsersAsync |
| `Application/UseCases/StoreHub/Users/CloudUserDto.cs` | Create | DTO for cloud users |

**ICloudSyncClient extension:**
```csharp
Task<CloudUserSyncResponse?> GetUsersAsync(
    string storeId,
    long? sinceVersion = null,
    CancellationToken ct = default);
```

**CloudUserDto:**
```csharp
public record CloudUserDto(
    Guid Id,
    string Username,
    string FirstName,
    string LastName,
    int RoleId,
    bool IsActive,
    long Version);
```

---

### Phase 5: StoreHub - UserSyncService

| File | Action | Description |
|------|--------|-------------|
| `Infrastructure/Services/StoreHub/UserSyncService.cs` | Create | Background sync logic |
| `Infrastructure/Services/StoreHub/UserSyncOptions.cs` | Create | Config options |
| `Infrastructure/ConfigureServices.cs` | Modify | Register services |

**UserSyncService logic:**
```csharp
public class UserSyncService : IUserSyncService
{
    public async Task<int> SyncUsersFromCloudAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var lastVersion = await GetLastSyncVersionAsync(ct);

        // Retry with exponential backoff (Polly or manual)
        var response = await _cloudClient.GetUsersAsync(_storeId, lastVersion, ct);

        if (response is null)
        {
            _logger.LogWarning("User sync failed - cloud unreachable");
            return 0; // Offline
        }

        foreach (var cloudUser in response.Users)
        {
            var storeUser = MapToStoreUser(cloudUser);
            // Explicit deactivation handling
            storeUser.IsActive = cloudUser.IsActive;
            await _userRepo.UpsertByCloudIdAsync(storeUser, ct);
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "User sync completed: {SyncedCount} users in {Duration}ms (version {FromVersion} → {ToVersion})",
            response.Count, stopwatch.ElapsedMilliseconds, lastVersion, response.MaxVersion);

        return response.Count;
    }
}
```

**Key behaviors:**
- Version-based sync (avoids clock drift)
- Explicit IsActive sync (deactivation enforced)
- Structured logging (count, duration, version range)
- Graceful offline handling

---

### Phase 6: Integration with SyncWorker

| File | Action | Description |
|------|--------|-------------|
| `Infrastructure/Services/StoreHub/SyncWorker.cs` | Modify | Call UserSyncService |
| `Infrastructure/Services/StoreHub/SyncWorkerOptions.cs` | Modify | Add UserSyncIntervalSeconds |

**SyncWorker enhancement:**
```csharp
// In ExecuteAsync loop:
if (ShouldSyncUsers())
{
    var synced = await _userSyncService.SyncUsersFromCloudAsync(ct);
    _logger.LogInformation("Synced {Count} users from cloud", synced);
}
```

---

### Phase 7: Tests

| File | Description |
|------|-------------|
| `Tests/CloudApi/Users/GetUsersEndpointTests.cs` | Endpoint tests |
| `Tests/StoreHub/Services/UserSyncServiceTests.cs` | Sync logic tests |

---

## File Summary

### New Files (~8)
| Layer | File |
|-------|------|
| CloudApi | `Domain/CloudUser.cs` |
| CloudApi | `Infrastructure/Configurations/CloudUserConfiguration.cs` |
| Application | `Abstractions/StoreHub/Services/IUserSyncService.cs` |
| Application | `UseCases/StoreHub/Users/CloudUserDto.cs` |
| Infrastructure | `Services/StoreHub/UserSyncService.cs` |
| Infrastructure | `Services/StoreHub/UserSyncOptions.cs` |
| Tests | `CloudApi/Users/GetUsersEndpointTests.cs` |
| Tests | `StoreHub/Services/UserSyncServiceTests.cs` |

### Modified Files (~7)
| File | Change |
|------|--------|
| `CloudApi/Infrastructure/CloudDbContext.cs` | Add CloudUser DbSet |
| `CloudApi/Program.cs` | Add /master/users endpoint |
| `Domain/Entities/Core/StoreUser.cs` | Add LastSyncedAtUtc |
| `Application/Abstractions/StoreHub/Services/ICloudSyncClient.cs` | Add GetUsersAsync |
| `Application/Abstractions/StoreHub/Repositories/IStoreUserRepository.cs` | Add UpsertByCloudIdAsync |
| `Infrastructure/Services/StoreHub/HttpCloudSyncClient.cs` | Implement GetUsersAsync |
| `Infrastructure/Persistence/StoreHub/Repositories/StoreUserRepository.cs` | Implement upsert |
| `Infrastructure/Services/StoreHub/SyncWorker.cs` | Call user sync |
| `Infrastructure/ConfigureServices.cs` | Register UserSyncService |

---

## Key Design Decisions

1. **No password sync** - Passwords managed locally, not sent over network
2. **CloudUserId linking** - Match users by Guid, not username (username can change)
3. **Version-based sync** - Use `sinceVersion` instead of timestamps (avoids clock drift)
4. **Soft delete** - Set `IsActive=false`, never hard delete
5. **Graceful offline** - If cloud unreachable, local auth continues working
6. **Store authorization** - CloudApi validates requesting store can only fetch its own users
7. **Explicit deactivation** - Sync explicitly enforces `IsActive=false` from cloud
8. **Structured logging** - Logs include count, duration, version range for debugging

---

## Out of Scope (Future Epics)

- User creation UI in CloudApi (admin tools)
- Password reset flow
- Multi-factor authentication
- User-initiated password change sync
- Pagination (stores have <100 users, not needed now)
- Batch database writes (small user counts, individual upserts acceptable)
- Rate limiting (internal service-to-service, trusted)

---

## Review Feedback Applied

| Suggestion | Status | Notes |
|------------|--------|-------|
| CloudVersion for sync safety | ✅ Applied | Added `Version` to CloudUser and StoreUser |
| Explicit response DTO | ✅ Applied | `CloudUserSyncResponse` record |
| Store authorization check | ✅ Applied | Validate storeId matches token |
| Explicit deactivation handling | ✅ Applied | Enforced in UserSyncService |
| Structured logging | ✅ Applied | Count, duration, version range |
| Pagination | ⏭️ Deferred | Stores have <100 users |
| Batch writes | ⏭️ Deferred | Small user counts |
| Rate limiting | ⏭️ Deferred | Internal service-to-service |
