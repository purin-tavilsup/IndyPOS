# Epic S1: POS Offline Authentication - Implementation Plan

**Status:** Approved (with review feedback incorporated)
**Author:** Claude + Pond
**Date:** 2026-03-10
**Reviewed by:** ChatGPT (2026-03-10)
**Reference:** `security/indypos_security_design_spec.md` (Sections 5, 6, 7)

---

## 1. Overview

Enable POS terminals to authenticate users locally via StoreHub, even when offline from the cloud.

### Goals
- Replace insecure TripleDES encryption with BCrypt hashing
- Add user authentication to StoreHub (local API)
- Desktop calls StoreHub for login instead of SQLite directly
- On-demand migration from legacy passwords

### Non-Goals (Future Epics)
- S2: Cloud user sync (CloudApi → StoreHub)
- S3: RBAC permission enforcement
- Account lockout after failed attempts
- MFA / passwordless authentication

---

## 2. Current State

### Existing Components
| Component | Location | Notes |
|-----------|----------|-------|
| `UserAccount` entity | `Domain/Entities/UserAccount.cs` | UserId, FirstName, LastName, RoleId |
| `UserCredential` entity | `Domain/Entities/UserCredential.cs` | UserId, Username, Password |
| `IUserLogInService` | `Application/Common/Interfaces/` | LogInAsync, LogOut |
| `UserLogInService` | `Infrastructure/Services/` | Current implementation |
| `CryptographyService` | `Infrastructure/Services/` | TripleDES encryption |
| `UserLogInPanel` | `Windows.Forms/UI/Login/` | Login form |
| `UserRoles` enum | `Application/Common/Enums/` | Cashier=1, StoreManager=2, SystemAdmin=3 |

### Security Issues 🔴
1. **Reversible encryption**: Passwords stored with TripleDES (can be decrypted)
2. **Hardcoded key**: UUID hash used as encryption key
3. **No salt**: Same password = same ciphertext
4. **ECB mode**: Pattern-preserving (insecure)

---

## 3. Architecture

### Target Flow
```
┌─────────────────┐     POST /auth/login     ┌─────────────────┐
│  POS (WinForms) │ ──────────────────────► │    StoreHub     │
│                 │ ◄────────────────────── │   (Local API)   │
│                 │        JWT Token         │                 │
└─────────────────┘                          └────────┬────────┘
                                                      │
                                               BCrypt verify
                                                      │
                                             ┌────────▼────────┐
                                             │   PostgreSQL    │
                                             │   (StoreUser)   │
                                             └─────────────────┘
```

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Password hashing | BCrypt (work factor 12) | Industry standard, already used in CloudApi |
| Migration strategy | On-demand during login | No downtime, gradual rollout |
| Session tokens | Local JWT (12-hour expiry) | Covers long shifts, avoids mid-shift logouts |
| Auth endpoint | StoreHub `/auth/login` | Aligns with Epic G (Desktop as StoreHub client) |
| Token storage | Memory + DPAPI fallback | Secure, re-login on app restart |
| HttpClient | IHttpClientFactory | Avoids socket exhaustion |

---

## 4. Implementation Phases

### Phase 1: Domain Layer

**New Files:**

| File | Description |
|------|-------------|
| `Domain/Entities/Core/StoreUser.cs` | User entity with password versioning |
| `Application/Abstractions/StoreHub/Services/IPasswordHasher.cs` | Hash/verify interface |
| `Application/Abstractions/StoreHub/Services/IStoreAuthService.cs` | Auth service interface |
| `Application/Abstractions/StoreHub/Repositories/IStoreUserRepository.cs` | Repository interface |

**StoreUser Entity:**
```csharp
public class StoreUser
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }               // Store scope (from IStoreIdentityService)
    public int LegacyUserId { get; set; }           // Migration link (UNIQUE constraint)
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int PasswordHashVersion { get; set; } = 2; // 1=TripleDES, 2=BCrypt
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime LastModifiedAtUtc { get; set; }
    public DateTime? LastLoginAtUtc { get; set; }
    public Guid? CloudUserId { get; set; }          // Link to CloudApi (S2)
}
```

**EF Core Constraints:**
```csharp
// In StoreUserConfiguration.cs
builder.HasIndex(x => x.LegacyUserId).IsUnique();  // Prevent duplicate migrations
builder.HasIndex(x => new { x.StoreId, x.Username }).IsUnique();  // Unique username per store
```

---

### Phase 2: Infrastructure Layer

**New Files:**

| File | Description |
|------|-------------|
| `Infrastructure/Services/StoreHub/BcryptPasswordHasher.cs` | BCrypt implementation |
| `Infrastructure/Persistence/StoreHub/Configurations/StoreUserConfiguration.cs` | EF Core config |
| `Infrastructure/Persistence/StoreHub/Repositories/StoreUserRepository.cs` | Repository |
| `Infrastructure/Services/StoreHub/StoreAuthService.cs` | Auth with migration |
| `Infrastructure/Services/StoreHub/LocalTokenService.cs` | JWT generation (12h expiry) |
| `Application/Abstractions/StoreHub/Services/ILocalTokenService.cs` | Token interface |
| `Application/UseCases/StoreHub/Auth/LoginRequest.cs` | Typed request DTO |

**Modified Files:**

| File | Change |
|------|--------|
| `StoreHub/IndyPOS.StoreHub.csproj` | Add BCrypt.Net-Next package |
| `Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs` | Add DbSet<StoreUser> |

**Migration Logic:**
```csharp
public async Task<AuthResult> AuthenticateAsync(string username, string password, CancellationToken ct)
{
    var user = await _userRepository.GetByUsernameAsync(username, ct);
    if (user is null || !user.IsActive)
        return AuthResult.Failed("Invalid credentials");

    bool isValid;

    if (user.PasswordHashVersion == 1) // Legacy TripleDES
    {
        var encryptedPassword = _legacyCrypto.Encrypt(password);
        isValid = encryptedPassword == user.PasswordHash;

        if (isValid)
        {
            // Upgrade to BCrypt
            var bcryptHash = _passwordHasher.Hash(password);
            await _userRepository.UpdatePasswordHashAsync(user.Id, bcryptHash, version: 2, ct);
            _logger.LogInformation("Migrated user {Username} to BCrypt", username);
        }
    }
    else // BCrypt (v2)
    {
        isValid = _passwordHasher.Verify(password, user.PasswordHash);
    }

    if (!isValid)
        return AuthResult.Failed("Invalid credentials");

    await _userRepository.UpdateLastLoginAsync(user.Id, DateTime.UtcNow, ct);
    var token = _tokenService.GenerateToken(user);
    return AuthResult.Success(token, user.ToDto());
}
```

---

### Phase 3: Application Layer (CQRS)

**New Files:**

| File | Description |
|------|-------------|
| `Application/UseCases/StoreHub/Auth/LoginCommand.cs` | Command + Response |
| `Application/UseCases/StoreHub/Auth/LoginCommandHandler.cs` | Handler |
| `Application/UseCases/StoreHub/Auth/StoreUserDto.cs` | User DTO |

---

### Phase 4: StoreHub API Endpoints

**Modified File:** `StoreHub/Program.cs`

**New Endpoints:**
```csharp
// POST /auth/login - Authenticate user (typed request DTO)
app.MapPost("/auth/login", async (
    LoginRequest request,  // public record LoginRequest(string Username, string Password);
    ICommandHandler<LoginCommand, LoginResponse> handler,
    CancellationToken ct) =>
{
    var command = new LoginCommand(request.Username, request.Password);
    var response = await handler.HandleAsync(command, ct);

    return response.Success
        ? Results.Ok(response)
        : Results.Unauthorized();
});

// POST /auth/logout - Invalidate session (optional for v1)
app.MapPost("/auth/logout", [Authorize] async (...) => { ... });

// GET /auth/me - Get current user info
app.MapGet("/auth/me", [Authorize] async (...) => { ... });
```

---

### Phase 5: Data Migration

**New File:** `Infrastructure/Persistence/StoreHub/Seeders/UserMigrationSeeder.cs`

Migrates existing users from SQLite to PostgreSQL:
1. Read `UserAccount` + `UserCredential` from SQLite
2. Create `StoreUser` records in PostgreSQL (with UPSERT to prevent duplicates)
3. Set `PasswordHashVersion = 1` (TripleDES)
4. BCrypt upgrade happens on first login

**UPSERT Logic (prevents duplicate migrations):**
```csharp
// Check if already migrated
var existing = await _storeUserRepo.GetByLegacyIdAsync(legacyUser.UserId, ct);
if (existing is not null)
{
    _logger.LogDebug("User {Username} already migrated, skipping", legacyUser.Username);
    continue;
}

// Create new StoreUser
var storeUser = new StoreUser
{
    Id = Guid.NewGuid(),
    StoreId = _storeIdentity.GetStoreId(),
    LegacyUserId = legacyUser.UserId,  // UNIQUE constraint prevents duplicates
    // ... rest of mapping
};
```

**Run on StoreHub startup (dev only):**
```csharp
if (app.Environment.IsDevelopment())
{
    await app.Services.GetRequiredService<UserMigrationSeeder>().MigrateAsync();
}
```

---

### Phase 6: Desktop UI Updates

**New File:** `Infrastructure/Services/StoreHubUserLogInService.cs`

Uses `IHttpClientFactory` (not raw `HttpClient`) to avoid socket exhaustion:

```csharp
public class StoreHubUserLogInService : IUserLogInService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEventAggregator _eventAggregator;
    private readonly ITokenStorage _tokenStorage;  // Memory + optional DPAPI

    public StoreHubUserLogInService(
        IHttpClientFactory httpClientFactory,
        IEventAggregator eventAggregator,
        ITokenStorage tokenStorage)
    {
        _httpClientFactory = httpClientFactory;
        _eventAggregator = eventAggregator;
        _tokenStorage = tokenStorage;
    }

    public async Task<bool> LogInAsync(string username, string password)
    {
        var client = _httpClientFactory.CreateClient("StoreHub");
        var response = await client.PostAsJsonAsync("/auth/login",
            new LoginRequest(username, password));

        if (!response.IsSuccessStatusCode)
            return false;

        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

        // Store token in memory (+ optional DPAPI encrypted fallback)
        _tokenStorage.SetToken(result.Token);

        // Publish logged-in event
        _eventAggregator.GetEvent<UserLoggedInEvent>().Publish(result.User);

        return true;
    }
}
```

**DI Registration:**
```csharp
services.AddHttpClient("StoreHub", client =>
{
    client.BaseAddress = new Uri("https://localhost:7001");
});
```

**Modified Files:**

| File | Change |
|------|--------|
| `Windows.Forms/UI/Login/UserLogInPanel.cs` | Remove encryption, send plain password |
| `Windows.Forms/UI/User/AddNewUserForm.cs` | Remove encryption |
| `Infrastructure/ConfigureServices.cs` | Register `StoreHubUserLogInService` |

---

### Phase 7: Tests

**New Files:**

| File | Test Cases |
|------|------------|
| `Tests/StoreHub/Auth/LoginCommandHandlerTests.cs` | Auth scenarios |
| `Tests/StoreHub/Auth/BcryptPasswordHasherTests.cs` | Hash/verify |

**Test Cases:**
1. ✅ Valid BCrypt login succeeds
2. ✅ Legacy TripleDES login migrates to BCrypt and succeeds
3. ✅ Invalid password fails
4. ✅ Inactive user fails
5. ✅ Non-existent user fails
6. ✅ Token contains correct claims (UserId, Role, StoreId)
7. ✅ BCrypt work factor produces expected timing (~250ms)

---

## 5. File Summary

### New Files (~12)
| Layer | File |
|-------|------|
| Domain | `Entities/Core/StoreUser.cs` |
| Application | `Abstractions/StoreHub/Services/IPasswordHasher.cs` |
| Application | `Abstractions/StoreHub/Services/IStoreAuthService.cs` |
| Application | `Abstractions/StoreHub/Services/ILocalTokenService.cs` |
| Application | `Abstractions/StoreHub/Repositories/IStoreUserRepository.cs` |
| Application | `UseCases/StoreHub/Auth/LoginCommand.cs` |
| Application | `UseCases/StoreHub/Auth/LoginCommandHandler.cs` |
| Application | `UseCases/StoreHub/Auth/StoreUserDto.cs` |
| Infrastructure | `Services/StoreHub/BcryptPasswordHasher.cs` |
| Infrastructure | `Services/StoreHub/StoreAuthService.cs` |
| Infrastructure | `Services/StoreHub/LocalTokenService.cs` |
| Infrastructure | `Persistence/StoreHub/Configurations/StoreUserConfiguration.cs` |
| Infrastructure | `Persistence/StoreHub/Repositories/StoreUserRepository.cs` |
| Infrastructure | `Persistence/StoreHub/Seeders/UserMigrationSeeder.cs` |
| Infrastructure | `Services/StoreHubUserLogInService.cs` |
| Tests | `StoreHub/Auth/LoginCommandHandlerTests.cs` |
| Tests | `StoreHub/Auth/BcryptPasswordHasherTests.cs` |

### Modified Files (~5)
| File | Change |
|------|--------|
| `StoreHub/IndyPOS.StoreHub.csproj` | Add BCrypt.Net-Next |
| `StoreHub/Program.cs` | Add /auth endpoints |
| `Infrastructure/Persistence/StoreHub/StoreHubDbContext.cs` | Add StoreUser |
| `Windows.Forms/UI/Login/UserLogInPanel.cs` | Remove encryption |
| `Infrastructure/ConfigureServices.cs` | Register new services |

---

## 6. Dependencies

```
Phase 1 (Domain)
    ↓
Phase 2 (Infrastructure)
    ↓
Phase 3 (Application) → Phase 4 (API) → Phase 5 (Migration)
                                              ↓
                                        Phase 6 (UI) → Phase 7 (Tests)
```

---

## 7. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Corrupted legacy passwords | Log failures, allow admin password reset |
| BCrypt too slow | Start with work factor 12, benchmark on target hardware |
| Token storage on WinForms | Use DPAPI or secure credential store |
| StoreHub not running | Show clear error message, don't allow offline SQLite fallback |

---

## 8. Security Checklist

- [ ] BCrypt work factor >= 12
- [ ] No plain passwords in logs
- [ ] HTTPS for localhost (dev certs)
- [ ] JWT signed with secure key
- [ ] Token expiry enforced
- [ ] Legacy `CryptographyService` removed after migration period

---

## 9. Rollout Plan

1. **Dev**: Implement and test with Aspire
2. **Test**: Migrate test store users, verify login
3. **Pilot**: Deploy to Store 1, monitor for 1 week
4. **Full**: Roll out to all stores
5. **Cleanup**: Remove `CryptographyService` after all users migrated

---

## 10. Open Questions (Resolved)

| Question | Answer | Source |
|----------|--------|--------|
| Token refresh for long shifts? | No refresh tokens needed, use 12h expiry | Review |
| Token storage in WinForms? | Memory + optional DPAPI fallback | Review |
| Migration timeout? | 90 days, then remove TripleDES code | Review |

---

## 11. Review Feedback Incorporated

| Feedback | Action Taken |
|----------|--------------|
| Token expiry: 12h instead of 8h | ✅ Updated |
| UNIQUE(LegacyUserId) constraint | ✅ Added to EF config |
| Add StoreId to StoreUser | ✅ Added field |
| Use IHttpClientFactory | ✅ Updated service |
| Add LoginRequest DTO | ✅ Added to Phase 2 |
| UPSERT in seeder | ✅ Added logic |

**Deferred to future epics:**
- Rate limiting (S8)
- AuthAuditLog (S7)
- TerminalId (Epic G)

---

**Approved by:** Pond
**Date:** 2026-03-10
