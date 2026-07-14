# Admin Provisioning — Single-Use Bootstrap Password + Forced Rotation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the wizard-typed admin password with a random, single-use bootstrap credential that is server-side force-rotated on first login, with a documented reset/recovery path.

**Architecture:** The installer generates a random admin password (Store-ID-only wizard), seeds it via the existing `migrate` path, then surgically removes the plaintext from `appsettings.json`. A `MustChangePassword` flag on `StoreUser` flows into a `must_change` JWT claim; a StoreHub middleware 403s every route except `/auth/change-password` until the user rotates. WinForms defers the logged-in session until rotation completes. A `reset-admin` CLI re-arms the flow for recovery/re-install.

**Tech Stack:** C# .NET 10, ASP.NET Core Minimal APIs, EF Core (Npgsql/PostgreSQL), WinForms, JWT (BCrypt password hashing), xUnit + Moq + FluentAssertions, Velopack + WinForms bootstrapper installer, IndyPOS.Vault (DPAPI).

## Global Constraints

- Target framework **.NET 10**; nullable reference types enabled.
- Password hashing is **BCrypt** (`PasswordHashVersion = 2`) via `IPasswordHasher`.
- New-password policy: **length ≥ 8** AND **≠ current password** (trimmed with `.Trim()`, whitespace-only rejected). No complexity rules (deliberate — see spec D5).
- Any date formatting uses **`CultureInfo.InvariantCulture`** (Thai-locale Buddhist-calendar hazard).
- DPAPI-protected `ConnectionStrings:storehub-db` and `LocalToken:SecretKey` must be preserved **byte-identical** by any `appsettings.json` rewrite.
- StoreHub JSON is camelCase; StoreHub reads user id from the token via `ClaimTypes.NameIdentifier` (the default inbound mapping remaps `sub`).
- Test framework per project: **xUnit** (`[Fact]`/`[Theory]`), **Moq**, and **FluentAssertions** in `IndyPOS.Bootstrapper.Tests`; plain `Assert.*` in `IndyPOS.Application.Tests`. Match the neighboring files in each project.
- Conventional commits; one logical unit per commit.
- Spec: `docs/superpowers/specs/2026-07-14-admin-provisioning-bootstrap-design.md`.

---

## File Structure

**Domain / Application (server contracts + use cases)**
- `src/IndyPOS.Domain/Entities/Core/StoreUser.cs` — add `MustChangePassword`.
- `src/IndyPOS.Application/Abstractions/StoreHub/Services/IStoreAuthService.cs` — `AuthResult` + flag.
- `src/IndyPOS.Application/UseCases/StoreHub/Auth/LoginResponse.cs` — add flag.
- `src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePassword/` — command, response, handler (new).
- `src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePasswordRequest.cs` — endpoint DTO (new).
- `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IStoreUserRepository.cs` — `SetPasswordAsync`.
- `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs` — `ChangePasswordAsync`.
- `src/IndyPOS.Application/Common/Interfaces/IUserLogInService.cs` — new return types + change method.
- `src/IndyPOS.Application/Common/Interfaces/LogInResult.cs` — `LogInResult`, `ChangePasswordResult` (new).
- `src/IndyPOS.Application/Common/Interfaces/IChangePasswordPrompt.cs` — prompt seam (new).
- `src/IndyPOS.Application/Common/Interfaces/IFirstLoginCoordinator.cs` — coordinator (new).

**Infrastructure (implementations)**
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreAuthService.cs` — populate flag.
- `src/IndyPOS.Infrastructure/Services/StoreHub/LocalTokenService.cs` — `must_change` claim.
- `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/StoreUserRepository.cs` — `SetPasswordAsync`.
- `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/StoreUserConfiguration.cs` — column map.
- `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/…` — generated migration (new).
- `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/InitialAdminSeeder.cs` — return seeded flag + `ResetAsync`.
- `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContextExtensions.cs` — return flag + `ResetAdminAsync`.
- `src/IndyPOS.Infrastructure/Services/StoreHub/AdminPasswordGenerator.cs` — random generator (new).
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs` — `ChangePasswordAsync`.
- `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubUserLogInService.cs` — deferred session + change method.
- `src/IndyPOS.Infrastructure/Services/StoreHub/FirstLoginCoordinator.cs` — coordinator (new).
- `src/IndyPOS.Infrastructure/ConfigureServices.cs` — register coordinator.

**StoreHub host**
- `src/IndyPOS.StoreHub/Program.cs` — `reset-admin` mode, seeded marker, `must_change` gate, change-password endpoint, `/auth/me` fix, handler registration.

**WinForms**
- `src/IndyPOS.Windows.Forms/UI/Login/ChangePasswordForm.cs` — modal (new).
- `src/IndyPOS.Windows.Forms/UI/Login/ChangePasswordPrompt.cs` — `IChangePasswordPrompt` impl (new).
- `src/IndyPOS.Windows.Forms/UI/Login/UserLogInPanel.cs` — use coordinator.
- WinForms DI registration (locate `AddSingleton<...>`/`AddScoped<...>` for forms).

**Installer (bootstrapper)**
- `installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs` — random `AdminPassword`.
- `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs` — Store-ID-only + finish screen.
- `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs` — `RemoveInitialAdminFromConfigAsync`.
- `installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs` — parse seeded marker.
- `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs` — clear step + result.

**Docs**
- `docs/operations/store-installation-guide.md` — bootstrap + reset-admin.

---

## Phase A — Server data model + flag threading

### Task A1: Add `MustChangePassword` to `StoreUser` + EF mapping + migration

**Files:**
- Modify: `src/IndyPOS.Domain/Entities/Core/StoreUser.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/StoreUserConfiguration.cs`
- Create: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/<timestamp>_AddMustChangePassword.cs` (generated)

**Interfaces:**
- Produces: `StoreUser.MustChangePassword` (`bool`), column `must_change_password NOT NULL DEFAULT false`.

- [ ] **Step 1: Add the property**

In `StoreUser.cs`, after `IsActive` (line 41):

```csharp
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When true, the user must rotate their password before any other action.
    /// Set on the seeded bootstrap admin; cleared on first successful change.
    /// </summary>
    public bool MustChangePassword { get; set; }
```

- [ ] **Step 2: Map the column**

In `StoreUserConfiguration.cs`, after the `IsActive` block (line 59):

```csharp
        builder.Property(e => e.MustChangePassword)
               .HasColumnName("must_change_password")
               .HasDefaultValue(false)
               .IsRequired();
```

- [ ] **Step 3: Generate the migration**

Run:
```bash
dotnet ef migrations add AddMustChangePassword \
  --project src/IndyPOS.Infrastructure \
  --startup-project src/IndyPOS.StoreHub \
  --context StoreHubDbContext
```
Expected: a new migration file under `src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/`.

- [ ] **Step 4: Verify the migration ships a SQL default**

Open the generated migration. The `Up` method MUST contain an `AddColumn<bool>` with `nullable: false` and `defaultValue: false`, e.g.:

```csharp
migrationBuilder.AddColumn<bool>(
    name: "must_change_password",
    table: "store_user",
    type: "boolean",
    nullable: false,
    defaultValue: false);
```
If `defaultValue: false` is missing, add it by hand (populated tables on the 3 existing stores would otherwise fail `ALTER TABLE ADD COLUMN NOT NULL`).

- [ ] **Step 5: Build**

Run: `dotnet build src/IndyPOS.Infrastructure`
Expected: build succeeds.

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Domain/Entities/Core/StoreUser.cs \
  src/IndyPOS.Infrastructure/Persistence/StoreHub/Configurations/StoreUserConfiguration.cs \
  src/IndyPOS.Infrastructure/Persistence/StoreHub/Migrations/
git commit -m "feat(storehub): add MustChangePassword to StoreUser + migration"
```

---

### Task A2: Thread the flag through `AuthResult` → `LoginResponse`

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Services/IStoreAuthService.cs`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Auth/LoginResponse.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreAuthService.cs:93`
- Modify: `src/IndyPOS.Application/UseCases/StoreHub/Auth/Login/LoginCommandHandler.cs:42`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Auth/StoreAuthServiceTests.cs`

**Interfaces:**
- Consumes: `StoreUser.MustChangePassword` (Task A1).
- Produces: `AuthResult.MustChangePassword` (`bool`), `AuthResult.Succeeded(token, user, mustChangePassword = false)`, `LoginResponse.MustChangePassword` (`bool`, default `false`).

- [ ] **Step 1: Write the failing test**

Add to `StoreAuthServiceTests.cs`:

```csharp
    [Fact]
    public async Task AuthenticateAsync_WhenUserMustChangePassword_ReturnsFlagTrue()
    {
        var password = "SecurePassword123!";
        var user = CreateTestUser(_passwordHasher.Hash(password), passwordVersion: 2);
        user.MustChangePassword = true;

        _userRepositoryMock
            .Setup(x => x.GetByUsernameAsync("testuser", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.AuthenticateAsync("testuser", password);

        Assert.True(result.Success);
        Assert.True(result.MustChangePassword);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter AuthenticateAsync_WhenUserMustChangePassword_ReturnsFlagTrue`
Expected: FAIL (compile error — `AuthResult` has no `MustChangePassword`).

- [ ] **Step 3: Add the flag to `AuthResult`**

In `IStoreAuthService.cs`, update the `AuthResult` record:

```csharp
public record AuthResult
{
    public bool Success { get; init; }
    public string? Token { get; init; }
    public AuthenticatedUser? User { get; init; }
    public string? ErrorMessage { get; init; }
    public bool MustChangePassword { get; init; }

    public static AuthResult Succeeded(string token, AuthenticatedUser user, bool mustChangePassword = false) =>
        new() { Success = true, Token = token, User = user, MustChangePassword = mustChangePassword };

    public static AuthResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}
```

- [ ] **Step 4: Populate the flag in `StoreAuthService`**

In `StoreAuthService.cs`, line 93, change the return to pass the flag:

```csharp
        return AuthResult.Succeeded(token, new AuthenticatedUser(
            Id: user.Id,
            Username: user.Username,
            FirstName: user.FirstName,
            LastName: user.LastName,
            RoleId: user.RoleId,
            StoreId: user.StoreId),
            mustChangePassword: user.MustChangePassword);
```

- [ ] **Step 5: Add the flag to `LoginResponse`**

Replace `LoginResponse.cs` body:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Auth;

/// <summary>
/// Response DTO for login endpoint. MustChangePassword lives on this envelope
/// (not StoreUserDto), so it does not leak into the session-long ILoggedInUser.
/// </summary>
public record LoginResponse(
    bool Success,
    string? Token,
    StoreUserDto? User,
    string? ErrorMessage,
    bool MustChangePassword = false);
```

- [ ] **Step 6: Pass the flag in `LoginCommandHandler`**

In `LoginCommandHandler.cs`, the success return (line 42) becomes:

```csharp
        return new LoginResponse(
            Success: true,
            Token: result.Token,
            User: userDto,
            ErrorMessage: null,
            MustChangePassword: result.MustChangePassword);
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter AuthenticateAsync_WhenUserMustChangePassword_ReturnsFlagTrue`
Expected: PASS.

- [ ] **Step 8: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/Services/IStoreAuthService.cs \
  src/IndyPOS.Application/UseCases/StoreHub/Auth/LoginResponse.cs \
  src/IndyPOS.Infrastructure/Services/StoreHub/StoreAuthService.cs \
  src/IndyPOS.Application/UseCases/StoreHub/Auth/Login/LoginCommandHandler.cs \
  tests/IndyPOS.Application.Tests/StoreHub/Auth/StoreAuthServiceTests.cs
git commit -m "feat(storehub): thread MustChangePassword through AuthResult and LoginResponse"
```

---

## Phase B — Server: claim, gate, change-password, reset CLI

### Task B1: Add `must_change` claim to the JWT when the flag is set

**Files:**
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/LocalTokenService.cs:44-57`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Auth/LocalTokenServiceTests.cs`

**Interfaces:**
- Produces: JWT claim `must_change` = `"true"` present **only** when `user.MustChangePassword`.

- [ ] **Step 1: Write the failing test**

Add to `LocalTokenServiceTests.cs` (match the existing constructor/setup in that file for `_sut`/options; if it builds tokens and reads them back, mirror that). Use a fresh service instance if the file has no shared `_sut`:

```csharp
    [Fact]
    public void GenerateToken_WhenMustChangePassword_IncludesMustChangeClaim()
    {
        var options = Options.Create(new LocalTokenOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "IndyPOS.Test",
            Audience = "IndyPOS.TestClient",
            ExpiryHours = 12
        });
        var sut = new LocalTokenService(options);
        var user = new StoreUser
        {
            Id = Guid.NewGuid(), Username = "admin", StoreId = "s1",
            FirstName = "A", LastName = "B", RoleId = 3,
            MustChangePassword = true
        };

        var token = sut.GenerateToken(user);

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Contains(jwt.Claims, c => c.Type == "must_change" && c.Value == "true");
    }

    [Fact]
    public void GenerateToken_WhenNotMustChange_OmitsMustChangeClaim()
    {
        var options = Options.Create(new LocalTokenOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "IndyPOS.Test",
            Audience = "IndyPOS.TestClient",
            ExpiryHours = 12
        });
        var sut = new LocalTokenService(options);
        var user = new StoreUser
        {
            Id = Guid.NewGuid(), Username = "cashier", StoreId = "s1",
            FirstName = "A", LastName = "B", RoleId = 1,
            MustChangePassword = false
        };

        var token = sut.GenerateToken(user);

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.DoesNotContain(jwt.Claims, c => c.Type == "must_change");
    }
```

Add `using IndyPOS.Domain.Entities.Core;`, `using IndyPOS.Application.Common.Models;`, `using Microsoft.Extensions.Options;` if not already present.

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter LocalTokenServiceTests`
Expected: the two new tests FAIL.

- [ ] **Step 3: Implement the claim**

In `LocalTokenService.cs`, replace the `claims` array (lines 48-57) with a list that conditionally adds the claim:

```csharp
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("role_id", user.RoleId.ToString()),
            new("store_id", user.StoreId),
            new("first_name", user.FirstName),
            new("last_name", user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.MustChangePassword)
        {
            claims.Add(new Claim("must_change", "true"));
        }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter LocalTokenServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.Infrastructure/Services/StoreHub/LocalTokenService.cs \
  tests/IndyPOS.Application.Tests/StoreHub/Auth/LocalTokenServiceTests.cs
git commit -m "feat(storehub): add must_change JWT claim for force-rotation gate"
```

---

### Task B2: Add `SetPasswordAsync` to the user repository

**Files:**
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IStoreUserRepository.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/StoreUserRepository.cs`
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ChangePasswordEndpointTests.cs` (created in B4 covers this end-to-end; add a focused repo test here in the integration project since it needs a real DbContext).

**Interfaces:**
- Produces: `Task SetPasswordAsync(Guid id, string newHash, bool mustChangePassword, CancellationToken cancellationToken = default)` — one atomic UPDATE of `PasswordHash`, `PasswordHashVersion=2`, `MustChangePassword`, `LastModifiedAtUtc`.

- [ ] **Step 1: Declare the method**

In `IStoreUserRepository.cs`, after `UpdatePasswordHashAsync` (line 43):

```csharp
    /// <summary>
    /// Sets a new BCrypt password hash and the must-change flag in one atomic write.
    /// Used by the change-password use case (clears the flag) and admin reset (sets it).
    /// </summary>
    Task SetPasswordAsync(Guid id, string newHash, bool mustChangePassword, CancellationToken cancellationToken = default);
```

- [ ] **Step 2: Implement it**

In `StoreUserRepository.cs`, after `UpdatePasswordHashAsync` (line 69):

```csharp
    public async Task SetPasswordAsync(Guid id, string newHash, bool mustChangePassword, CancellationToken cancellationToken = default)
    {
        await _dbContext.StoreUsers
                        .Where(u => u.Id == id)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(u => u.PasswordHash, newHash)
                            .SetProperty(u => u.PasswordHashVersion, 2)
                            .SetProperty(u => u.MustChangePassword, mustChangePassword)
                            .SetProperty(u => u.LastModifiedAtUtc, DateTime.UtcNow),
                            cancellationToken);
    }
```

- [ ] **Step 3: Build**

Run: `dotnet build src/IndyPOS.Infrastructure`
Expected: build succeeds. (Behavioural coverage arrives via the B4 endpoint test, which re-queries the DB.)

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.Application/Abstractions/StoreHub/Repositories/IStoreUserRepository.cs \
  src/IndyPOS.Infrastructure/Persistence/StoreHub/Repositories/StoreUserRepository.cs
git commit -m "feat(storehub): add atomic SetPasswordAsync to user repository"
```

---

### Task B3: `ChangePassword` command + handler

**Files:**
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePassword/ChangePasswordCommand.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePassword/ChangePasswordResponse.cs`
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePassword/ChangePasswordCommandHandler.cs`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Auth/ChangePasswordCommandHandlerTests.cs`

**Interfaces:**
- Consumes: `IStoreUserRepository.GetByIdAsync`, `IStoreUserRepository.SetPasswordAsync` (Task B2), `IPasswordHasher`, `ILocalTokenService.GenerateToken` (Task B1).
- Produces: `ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword) : ICommand<ChangePasswordResponse>`; `ChangePasswordResponse(bool Success, string? Token, string? ErrorMessage)`.

- [ ] **Step 1: Write the command + response**

`ChangePasswordCommand.cs`:

```csharp
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;

/// <summary>
/// Rotates the authenticated user's password. UserId comes from the JWT, never
/// the request body.
/// </summary>
public record ChangePasswordCommand(Guid UserId, string CurrentPassword, string NewPassword)
    : ICommand<ChangePasswordResponse>;
```

`ChangePasswordResponse.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;

/// <summary>
/// On success, Token is a fresh JWT WITHOUT the must_change claim so the caller
/// can proceed with a normal session.
/// </summary>
public record ChangePasswordResponse(bool Success, string? Token, string? ErrorMessage);
```

- [ ] **Step 2: Write the failing tests**

`ChangePasswordCommandHandlerTests.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Application.Common.Models;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IStoreUserRepository> _repo = new();
    private readonly IPasswordHasher _hasher = new BcryptPasswordHasher();
    private readonly ILocalTokenService _tokens;
    private readonly ChangePasswordCommandHandler _sut;

    public ChangePasswordCommandHandlerTests()
    {
        _tokens = new LocalTokenService(Options.Create(new LocalTokenOptions
        {
            SecretKey = "TestSecretKeyThatIsAtLeast32Characters!",
            Issuer = "IndyPOS.Test",
            Audience = "IndyPOS.TestClient",
            ExpiryHours = 12
        }));
        _sut = new ChangePasswordCommandHandler(_repo.Object, _hasher, _tokens);
    }

    private StoreUser MakeUser(string currentPassword)
    {
        var user = new StoreUser
        {
            Id = Guid.NewGuid(), Username = "admin", StoreId = "s1",
            FirstName = "A", LastName = "B", RoleId = 3,
            PasswordHash = _hasher.Hash(currentPassword), PasswordHashVersion = 2,
            MustChangePassword = true
        };
        _repo.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidChange_ClearsFlagAndReturnsFreshToken()
    {
        var user = MakeUser("oldPass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "oldPass123", "brandNew123"));

        Assert.True(result.Success);
        Assert.NotNull(result.Token);
        _repo.Verify(r => r.SetPasswordAsync(user.Id, It.IsAny<string>(), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithWrongCurrentPassword_FailsAndDoesNotWrite()
    {
        var user = MakeUser("oldPass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "WRONG", "brandNew123"));

        Assert.False(result.Success);
        _repo.Verify(r => r.SetPasswordAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTooShortNewPassword_Fails()
    {
        var user = MakeUser("oldPass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "oldPass123", "short7!"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Handle_WhenNewEqualsCurrent_Fails()
    {
        var user = MakeUser("samePass123");
        var result = await _sut.HandleAsync(new ChangePasswordCommand(user.Id, "samePass123", "samePass123"));

        Assert.False(result.Success);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_Fails()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((StoreUser?)null);
        var result = await _sut.HandleAsync(new ChangePasswordCommand(Guid.NewGuid(), "x", "brandNew123"));

        Assert.False(result.Success);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter ChangePasswordCommandHandlerTests`
Expected: FAIL (handler not defined).

- [ ] **Step 4: Implement the handler**

`ChangePasswordCommandHandler.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using Nokpirab;

namespace IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;

public class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private const int MinPasswordLength = 8;

    private readonly IStoreUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ILocalTokenService _tokens;

    public ChangePasswordCommandHandler(
        IStoreUserRepository users,
        IPasswordHasher hasher,
        ILocalTokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<ChangePasswordResponse> HandleAsync(
        ChangePasswordCommand command,
        CancellationToken cancellationToken = default)
    {
        var newPassword = command.NewPassword?.Trim() ?? string.Empty;
        var currentPassword = command.CurrentPassword ?? string.Empty;

        if (newPassword.Length < MinPasswordLength)
        {
            return new ChangePasswordResponse(false, null, $"New password must be at least {MinPasswordLength} characters.");
        }

        if (newPassword == currentPassword.Trim())
        {
            return new ChangePasswordResponse(false, null, "New password must be different from the current password.");
        }

        var user = await _users.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return new ChangePasswordResponse(false, null, "User not found.");
        }

        if (!_hasher.Verify(currentPassword, user.PasswordHash))
        {
            return new ChangePasswordResponse(false, null, "Current password is incorrect.");
        }

        var newHash = _hasher.Hash(newPassword);
        await _users.SetPasswordAsync(user.Id, newHash, mustChangePassword: false, cancellationToken);

        // Mint a fresh token with the flag now cleared so the client gets a normal session.
        user.PasswordHash = newHash;
        user.MustChangePassword = false;
        var token = _tokens.GenerateToken(user);

        return new ChangePasswordResponse(true, token, null);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter ChangePasswordCommandHandlerTests`
Expected: PASS (5 tests).

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePassword/ \
  tests/IndyPOS.Application.Tests/StoreHub/Auth/ChangePasswordCommandHandlerTests.cs
git commit -m "feat(storehub): add ChangePassword command and handler"
```

---

### Task B4: `/auth/change-password` endpoint + `/auth/me` claim fix + handler registration

**Files:**
- Create: `src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePasswordRequest.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs` (register handler ~line 79; add endpoint after `/auth/me`; fix `/auth/me` userId)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ChangePasswordEndpointTests.cs`

**Interfaces:**
- Consumes: `ChangePasswordCommand`/`Response` (B3).
- Produces: `ChangePasswordRequest(string CurrentPassword, string NewPassword)`; `POST /auth/change-password` returning `200 {token}` or `400/401`.

- [ ] **Step 1: Write the request DTO**

`ChangePasswordRequest.cs`:

```csharp
namespace IndyPOS.Application.UseCases.StoreHub.Auth;

/// <summary>Request body for POST /auth/change-password. Identity is taken from the JWT.</summary>
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
```

- [ ] **Step 2: Write the failing integration tests**

`ChangePasswordEndpointTests.cs` (mirror `ProductsEndpointTests` / `IntegrationTestBase` patterns; `AuthenticateAsAsync` seeds a normal user with `MustChangePassword=false`, which is fine for the auth-required cases):

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

public class ChangePasswordEndpointTests : IntegrationTestBase
{
    public ChangePasswordEndpointTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    [Fact]
    public async Task ChangePassword_Unauthenticated_Returns401()
    {
        ClearAuthentication();
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "x", newPassword = "brandNew123" });

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_ReturnsError_AndDoesNotChange()
    {
        await AuthenticateAsAsync("changepw_wrong", "Password123!", UserRole.SystemAdmin);
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "WRONG", newPassword = "brandNew123" });

        Assert.False(resp.IsSuccessStatusCode);
    }

    [Fact]
    public async Task ChangePassword_TooShort_ReturnsBadRequest()
    {
        await AuthenticateAsAsync("changepw_short", "Password123!", UserRole.SystemAdmin);
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "Password123!", newPassword = "short7!" });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Valid_PersistsNewHash_AndClearsFlag()
    {
        await AuthenticateAsAsync("changepw_ok", "Password123!", UserRole.SystemAdmin);
        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "Password123!", newPassword = "brandNew123" });

        Assert.True(resp.IsSuccessStatusCode);

        await using var scope = Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
        var user = await db.StoreUsers.AsNoTracking().FirstAsync(u => u.Username == "changepw_ok");
        Assert.False(user.MustChangePassword);
        Assert.True(BCrypt.Net.BCrypt.Verify("brandNew123", user.PasswordHash));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter ChangePasswordEndpointTests`
Expected: FAIL (endpoint 404 / not found). NOTE: integration tests need Docker/Testcontainers-backed Postgres; if Docker is unavailable this run errors environmentally — proceed and rely on the VM/CI run, but still author the tests.

- [ ] **Step 4: Register the handler**

In `Program.cs`, after line 79 (`AddTransient<...LoginCommand...>`):

```csharp
builder.Services.AddTransient<ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>, ChangePasswordCommandHandler>();
```

Add usings at the top:
```csharp
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using System.Security.Claims;
```

- [ ] **Step 5: Add the endpoint + fix `/auth/me`**

In `Program.cs`, replace the `/auth/me` `userId` line (213) so it reads the mapped claim first:

```csharp
        userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value,
```

Immediately after the `/auth/me` endpoint block (after line 220), add:

```csharp
app.MapPost("/auth/change-password", async (
    ICommandHandler<ChangePasswordCommand, ChangePasswordResponse> handler,
    HttpContext context,
    ChangePasswordRequest request,
    CancellationToken cancellationToken) =>
{
    var idValue = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value;

    if (!Guid.TryParse(idValue, out var userId))
    {
        return Results.Unauthorized();
    }

    var command = new ChangePasswordCommand(userId, request.CurrentPassword, request.NewPassword);
    var response = await handler.HandleAsync(command, cancellationToken);

    return response.Success
        ? Results.Ok(response)
        : Results.BadRequest(new { error = response.ErrorMessage });
}).RequireAuthorization();
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter ChangePasswordEndpointTests`
Expected: PASS (Docker required). If environmental-only failure, record it and continue.

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/UseCases/StoreHub/Auth/ChangePasswordRequest.cs \
  src/IndyPOS.StoreHub/Program.cs \
  tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/ChangePasswordEndpointTests.cs
git commit -m "feat(storehub): add /auth/change-password endpoint and fix /auth/me user id claim"
```

---

### Task B5: `must_change` authorization gate middleware

**Files:**
- Modify: `src/IndyPOS.StoreHub/Program.cs` (after `app.UseAuthorization()`, line 178)
- Test: `tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/MustChangeGateTests.cs`

**Interfaces:**
- Produces: any authenticated request carrying `must_change=true` gets **403** unless the path is `/auth/change-password`.

- [ ] **Step 1: Write the failing tests**

`MustChangeGateTests.cs` — seed an admin with the flag set, log in to get a must-change token, then probe endpoints:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IndyPOS.Application.Common.Enums;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IndyPOS.StoreHub.IntegrationTests.Endpoints;

public class MustChangeGateTests : IntegrationTestBase
{
    public MustChangeGateTests(StoreHubWebApplicationFactory factory) : base(factory) { }

    private async Task<string> LoginAsMustChangeAdminAsync()
    {
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StoreHubDbContext>();
            db.StoreUsers.Add(new StoreUser
            {
                Id = Guid.NewGuid(), StoreId = "test-store",
                LegacyUserId = Random.Shared.Next(1000, 9999),
                Username = "mc_admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                PasswordHashVersion = 2, FirstName = "MC", LastName = "Admin",
                RoleId = (int)UserRole.SystemAdmin, IsActive = true,
                MustChangePassword = true,
                CreatedAtUtc = DateTime.UtcNow, LastModifiedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var login = await Client.PostAsJsonAsync("/auth/login", new { username = "mc_admin", password = "Password123!" });
        login.EnsureSuccessStatusCode();
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.True(body!.MustChangePassword);
        return body.Token!;
    }

    [Fact]
    public async Task MustChangeToken_IsBlockedOnProducts()
    {
        var token = await LoginAsMustChangeAdminAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await Client.GetAsync("/products");

        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task MustChangeToken_IsAllowedOnChangePassword()
    {
        var token = await LoginAsMustChangeAdminAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await Client.PostAsJsonAsync("/auth/change-password",
            new { currentPassword = "Password123!", newPassword = "brandNew123" });

        Assert.True(resp.IsSuccessStatusCode);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter MustChangeGateTests`
Expected: `MustChangeToken_IsBlockedOnProducts` FAILS (returns 200, not 403). Docker required.

- [ ] **Step 3: Implement the gate**

In `Program.cs`, immediately after `app.UseAuthorization();` (line 178):

```csharp
// Force-rotation gate: a token carrying must_change may reach ONLY the
// change-password endpoint. This is the server-side teeth behind the WinForms
// first-login flow — a dismissed dialog or a rogue client cannot bypass it.
app.Use(async (context, next) =>
{
    var mustChange = context.User.FindFirst("must_change")?.Value == "true";
    if (mustChange && !context.Request.Path.StartsWithSegments("/auth/change-password"))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new { error = "Password change required before continuing." });
        return;
    }

    await next();
});
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.StoreHub.IntegrationTests --filter MustChangeGateTests`
Expected: PASS (Docker required).

- [ ] **Step 5: Commit**

```bash
git add src/IndyPOS.StoreHub/Program.cs tests/IndyPOS.StoreHub.IntegrationTests/Endpoints/MustChangeGateTests.cs
git commit -m "feat(storehub): enforce must_change gate on all routes except change-password"
```

---

### Task B6: Seeder returns seeded/skipped + `ResetAsync`; random password generator

**Files:**
- Create: `src/IndyPOS.Infrastructure/Services/StoreHub/AdminPasswordGenerator.cs`
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/InitialAdminSeeder.cs`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Auth/AdminPasswordGeneratorTests.cs`

**Interfaces:**
- Produces: `AdminPasswordGenerator.Generate()` → 14-char unambiguous random string; `InitialAdminSeeder.SeedAsync(...)` → `Task<bool>` (true when it seeded); `InitialAdminSeeder.ResetAsync(string newPassword, CancellationToken)` (upserts `admin`, sets `MustChangePassword=true`).

- [ ] **Step 1: Write the failing generator test**

`AdminPasswordGeneratorTests.cs`:

```csharp
using IndyPOS.Infrastructure.Services.StoreHub;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class AdminPasswordGeneratorTests
{
    [Fact]
    public void Generate_ProducesUnambiguous14CharPassword()
    {
        var pw = AdminPasswordGenerator.Generate();

        Assert.Equal(14, pw.Length);
        Assert.DoesNotContain(pw, c => "0O1lI".Contains(c));
    }

    [Fact]
    public void Generate_ProducesDifferentValuesEachCall()
    {
        Assert.NotEqual(AdminPasswordGenerator.Generate(), AdminPasswordGenerator.Generate());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter AdminPasswordGeneratorTests`
Expected: FAIL (type not defined).

- [ ] **Step 3: Implement the generator**

`AdminPasswordGenerator.cs`:

```csharp
using System.Security.Cryptography;

namespace IndyPOS.Infrastructure.Services.StoreHub;

/// <summary>
/// Generates a human-typable random admin password. Excludes ambiguous glyphs
/// (0/O/1/l/I). ~14 chars from a 56-char alphabet ≈ 81 bits — plenty for a
/// single-use bootstrap credential that is force-rotated on first login.
/// </summary>
public static class AdminPasswordGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
    private const int Length = 14;

    public static string Generate()
    {
        var chars = new char[Length];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
        return new string(chars);
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter AdminPasswordGeneratorTests`
Expected: PASS.

- [ ] **Step 5: Write the failing seeder tests**

Add to `InitialAdminSeeder` a testable surface. Create `tests/IndyPOS.Application.Tests/StoreHub/Auth/InitialAdminSeederTests.cs`:

```csharp
using IndyPOS.Application.Abstractions.StoreHub.Repositories;
using IndyPOS.Application.Abstractions.StoreHub.Services;
using IndyPOS.Domain.Entities.Core;
using IndyPOS.Infrastructure.Persistence.StoreHub.Seeders;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Auth;

public class InitialAdminSeederTests
{
    private readonly Mock<IStoreUserRepository> _repo = new();
    private readonly IPasswordHasher _hasher = new BcryptPasswordHasher();
    private readonly Mock<IStoreIdentityService> _identity = new();
    private readonly Mock<ILogger<InitialAdminSeeder>> _logger = new();

    private InitialAdminSeeder Build(string? username, string? password)
    {
        _identity.SetupGet(i => i.StoreId).Returns("STORE-1");
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["InitialAdmin:Username"] = username,
            ["InitialAdmin:Password"] = password
        }).Build();
        return new InitialAdminSeeder(_repo.Object, _hasher, _identity.Object, config, _logger.Object);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminAbsent_SeedsWithMustChangeAndReturnsTrue()
    {
        _repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync((StoreUser?)null);
        var sut = Build("admin", "bootstrapPW123");

        var seeded = await sut.SeedAsync();

        Assert.True(seeded);
        _repo.Verify(r => r.AddAsync(It.Is<StoreUser>(u => u.MustChangePassword), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenAdminExists_ReturnsFalse()
    {
        _repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>()))
             .ReturnsAsync(new StoreUser { Username = "admin" });
        var sut = Build("admin", "bootstrapPW123");

        var seeded = await sut.SeedAsync();

        Assert.False(seeded);
        _repo.Verify(r => r.AddAsync(It.IsAny<StoreUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenNoConfig_ReturnsFalse()
    {
        var sut = Build(null, null);
        var seeded = await sut.SeedAsync();
        Assert.False(seeded);
    }

    [Fact]
    public async Task ResetAsync_WhenAdminExists_SetsPasswordAndMustChange()
    {
        var existing = new StoreUser { Id = Guid.NewGuid(), Username = "admin" };
        _repo.Setup(r => r.GetByUsernameAsync("admin", It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var sut = Build("admin", "ignored");

        await sut.ResetAsync("newBootstrap123");

        _repo.Verify(r => r.SetPasswordAsync(existing.Id, It.IsAny<string>(), true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

- [ ] **Step 6: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter InitialAdminSeederTests`
Expected: FAIL (`SeedAsync` returns `Task`, not `Task<bool>`; no `ResetAsync`).

- [ ] **Step 7: Update the seeder**

In `InitialAdminSeeder.cs`, change `SeedAsync` to return `Task<bool>`, set the flag on the seeded admin, and add `ResetAsync`. Replace the `SeedAsync` method and add `ResetAsync`:

```csharp
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
```

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter "InitialAdminSeederTests|AdminPasswordGeneratorTests"`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.Infrastructure/Services/StoreHub/AdminPasswordGenerator.cs \
  src/IndyPOS.Infrastructure/Persistence/StoreHub/Seeders/InitialAdminSeeder.cs \
  tests/IndyPOS.Application.Tests/StoreHub/Auth/AdminPasswordGeneratorTests.cs \
  tests/IndyPOS.Application.Tests/StoreHub/Auth/InitialAdminSeederTests.cs
git commit -m "feat(storehub): seeder reports seeded flag + admin ResetAsync + password generator"
```

---

### Task B7: Wire `reset-admin` CLI + seeded marker in `migrate` mode

**Files:**
- Modify: `src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContextExtensions.cs`
- Modify: `src/IndyPOS.StoreHub/Program.cs` (migrate/reset branches, lines 166-171)

**Interfaces:**
- Produces: `SeedInitialAdminAsync` → `Task<bool>`; `ResetAdminAsync` extension; `migrate` prints `ADMIN_SEEDED=<true|false>` to stdout; new `reset-admin` arg prints the generated password.

- [ ] **Step 1: Update the host extensions**

In `StoreHubDbContextExtensions.cs`, change `SeedInitialAdminAsync` to return the flag and add `ResetAdminAsync`:

```csharp
    public static async Task<bool> SeedInitialAdminAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<InitialAdminSeeder>();
        return await seeder.SeedAsync();
    }

    /// <summary>
    /// Recovery entry point for the "reset-admin" CLI: generates a fresh random
    /// password, (re)sets the admin with must-change, and returns the password
    /// so the caller can print it once.
    /// </summary>
    public static async Task<string> ResetAdminAsync(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<InitialAdminSeeder>();
        var password = IndyPOS.Infrastructure.Services.StoreHub.AdminPasswordGenerator.Generate();
        await seeder.ResetAsync(password);
        return password;
    }
```

- [ ] **Step 2: Update the CLI branches in `Program.cs`**

Replace the `migrate` branch (lines 166-171) with both branches:

```csharp
else if (Array.Exists(args, a => string.Equals(a, "migrate", StringComparison.OrdinalIgnoreCase)))
{
    await app.MigrateStoreHubDatabaseAsync();
    var seeded = await app.SeedInitialAdminAsync();
    // Marker consumed by the bootstrapper to decide the finish-screen credential text.
    Console.WriteLine($"ADMIN_SEEDED={(seeded ? "true" : "false")}");
    return;
}
else if (Array.Exists(args, a => string.Equals(a, "reset-admin", StringComparison.OrdinalIgnoreCase)))
{
    await app.MigrateStoreHubDatabaseAsync();
    var newPassword = await app.ResetAdminAsync();
    Console.WriteLine($"ADMIN_RESET=true");
    Console.WriteLine($"New admin password (change it on next sign-in): {newPassword}");
    return;
}
```

- [ ] **Step 3: Build**

Run: `dotnet build src/IndyPOS.StoreHub`
Expected: build succeeds. (CLI behaviour is validated in the VM run, Task E2.)

- [ ] **Step 4: Commit**

```bash
git add src/IndyPOS.Infrastructure/Persistence/StoreHub/StoreHubDbContextExtensions.cs src/IndyPOS.StoreHub/Program.cs
git commit -m "feat(storehub): reset-admin CLI + ADMIN_SEEDED marker in migrate mode"
```

---

## Phase C — WinForms client

### Task C1: New login/change result types + `IStoreHubClient.ChangePasswordAsync` + deferred session

**Files:**
- Create: `src/IndyPOS.Application/Common/Interfaces/LogInResult.cs`
- Modify: `src/IndyPOS.Application/Common/Interfaces/IUserLogInService.cs`
- Modify: `src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs`
- Modify: `src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubUserLogInService.cs`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubUserLogInServiceTests.cs`

**Interfaces:**
- Produces: `LogInResult(bool Success, bool MustChangePassword)`; `ChangePasswordResult(bool Success, string? ErrorMessage)`; `IUserLogInService.LogInAsync → Task<LogInResult>`; `IUserLogInService.ChangePasswordAsync(string,string) → Task<ChangePasswordResult>`; `IStoreHubClient.ChangePasswordAsync(string,string,CancellationToken) → Task<ChangePasswordResponse>`.
- Consumes: `ChangePasswordResponse` (B3), `LoginResponse.MustChangePassword` (A2).

- [ ] **Step 1: Write the result types**

`LogInResult.cs`:

```csharp
namespace IndyPOS.Application.Common.Interfaces;

/// <summary>Outcome of a login attempt. MustChangePassword true means the session
/// is NOT yet established — the caller must rotate the password first.</summary>
public record LogInResult(bool Success, bool MustChangePassword);

/// <summary>Outcome of a password change.</summary>
public record ChangePasswordResult(bool Success, string? ErrorMessage);
```

- [ ] **Step 2: Update `IUserLogInService`**

Replace `IUserLogInService.cs`:

```csharp
namespace IndyPOS.Application.Common.Interfaces;

public interface IUserLogInService
{
    Task<LogInResult> LogInAsync(string username, string password);

    Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword);

    void LogOut();
}
```

- [ ] **Step 3: Add `ChangePasswordAsync` to `IStoreHubClient`**

In `IStoreHubClient.cs`, after `LoginAsync` (line 21), add (and add `using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;` at the top):

```csharp
    /// <summary>
    /// Change the authenticated user's password. On success the returned token is
    /// a fresh JWT without the must_change claim.
    /// </summary>
    Task<ChangePasswordResponse> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken cancellationToken = default);
```

- [ ] **Step 4: Write the failing tests**

`StoreHubUserLogInServiceTests.cs` (mock `IStoreHubClient`, `IProductCacheService`, `IEventAggregator`, `IOptions<StoreHubOptions>`, `ILogger`; verify the event publish is deferred on must-change). Match how other service tests build `IEventAggregator` (Moq). Reference existing `UserLoggedInEvent` usage:

```csharp
using IndyPOS.Application.Abstractions.StoreHub;
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Application.Events;
using IndyPOS.Application.UseCases.StoreHub.Auth;
using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;
using IndyPOS.Infrastructure.Services.StoreHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class StoreHubUserLogInServiceTests
{
    private readonly Mock<IStoreHubClient> _client = new();
    private readonly Mock<IProductCacheService> _cache = new();
    private readonly Mock<IEventAggregator> _events = new();
    private readonly Mock<UserLoggedInEvent> _loggedInEvent = new();
    private readonly StoreHubUserLogInService _sut;

    public StoreHubUserLogInServiceTests()
    {
        _events.Setup(e => e.GetEvent<UserLoggedInEvent>()).Returns(_loggedInEvent.Object);
        var options = Options.Create(new StoreHubOptions { AutoSyncProductsOnStartup = false });
        _sut = new StoreHubUserLogInService(_client.Object, _cache.Object, _events.Object, options,
            new Mock<ILogger<StoreHubUserLogInService>>().Object);
    }

    private static LoginResponse Ok(bool mustChange) =>
        new(true, "token", new StoreUserDto(Guid.NewGuid(), "admin", "A", "B", 3, "s1"), null, mustChange);

    [Fact]
    public async Task LogInAsync_NormalLogin_PublishesLoggedInEvent()
    {
        _client.Setup(c => c.LoginAsync("admin", "pw", It.IsAny<CancellationToken>())).ReturnsAsync(Ok(false));

        var result = await _sut.LogInAsync("admin", "pw");

        Assert.True(result.Success);
        Assert.False(result.MustChangePassword);
        _loggedInEvent.Verify(e => e.Publish(It.IsAny<ILoggedInUser>()), Times.Once);
    }

    [Fact]
    public async Task LogInAsync_MustChange_DoesNotPublishLoggedInEvent()
    {
        _client.Setup(c => c.LoginAsync("admin", "pw", It.IsAny<CancellationToken>())).ReturnsAsync(Ok(true));

        var result = await _sut.LogInAsync("admin", "pw");

        Assert.True(result.Success);
        Assert.True(result.MustChangePassword);
        _loggedInEvent.Verify(e => e.Publish(It.IsAny<ILoggedInUser>()), Times.Never);
    }

    [Fact]
    public async Task ChangePasswordAsync_OnSuccess_SetsFreshToken()
    {
        _client.Setup(c => c.ChangePasswordAsync("old", "brandNew123", It.IsAny<CancellationToken>()))
               .ReturnsAsync(new ChangePasswordResponse(true, "fresh-token", null));

        var result = await _sut.ChangePasswordAsync("old", "brandNew123");

        Assert.True(result.Success);
        _client.Verify(c => c.SetAuthToken("fresh-token"), Times.Once);
    }
}
```

If `UserLoggedInEvent.Publish` is not virtual/mockable, instead assert via a spy `IEventAggregator` implementation; adjust to the project's `IEventAggregator` test pattern.

- [ ] **Step 5: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter StoreHubUserLogInServiceTests`
Expected: FAIL (signatures changed / `ChangePasswordAsync` missing).

- [ ] **Step 6: Implement `StoreHubHttpClient.ChangePasswordAsync`**

In `StoreHubHttpClient.cs`, after `LoginAsync` (line 92), add (add `using IndyPOS.Application.UseCases.StoreHub.Auth.ChangePassword;`):

```csharp
    public async Task<ChangePasswordResponse> ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();

        try
        {
            using var request = CreateAuthenticatedRequest(HttpMethod.Post, "/auth/change-password");
            request.Content = JsonContent.Create(
                new ChangePasswordRequest(currentPassword, newPassword), options: JsonOptions);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadFromJsonAsync<ChangePasswordResponse>(JsonOptions, cancellationToken);
                return body ?? new ChangePasswordResponse(false, null, "Invalid response from server");
            }

            var errorBody = await ReadResponseBodyAsync(response, cancellationToken);
            return new ChangePasswordResponse(false, null, $"Change password failed: {response.StatusCode}. {errorBody}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during change-password");
            return new ChangePasswordResponse(false, null, "Cannot connect to StoreHub.");
        }
    }
```

Add `using IndyPOS.Application.UseCases.StoreHub.Auth;` if not present (for `ChangePasswordRequest`).

- [ ] **Step 7: Implement the service changes**

In `StoreHubUserLogInService.cs`, replace `LogInAsync` (lines 36-76) so it defers publish/sync on must-change and returns `LogInResult`, and add `ChangePasswordAsync`:

```csharp
    public async Task<LogInResult> LogInAsync(string username, string password)
    {
        try
        {
            _logger.LogInformation("Attempting StoreHub login for user: {Username}", username);

            var response = await _storeHubClient.LoginAsync(username, password);

            if (!response.Success || response.User is null)
            {
                _logger.LogWarning("Login failed for user: {Username}. Error: {Error}", username, response.ErrorMessage);
                return new LogInResult(false, false);
            }

            // Must-change: the client already holds the restricted token (LoginAsync set it),
            // but we DO NOT establish the session — no product sync, no UserLoggedInEvent —
            // until the password is rotated. The coordinator drives the change then re-logs in.
            if (response.MustChangePassword)
            {
                _logger.LogInformation("User {Username} must change password before continuing.", username);
                return new LogInResult(true, true);
            }

            if (_options.AutoSyncProductsOnStartup)
            {
                await SyncProductsAsync();
            }

            _eventAggregator.GetEvent<UserLoggedInEvent>().Publish(new StoreHubLoggedInUser(response.User));
            _logger.LogInformation("Login successful for user: {Username}", username);
            return new LogInResult(true, false);
        }
        catch (StoreHubClientException ex)
        {
            _logger.LogError(ex, "StoreHub connection error during login");
            return new LogInResult(false, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during login");
            return new LogInResult(false, false);
        }
    }

    public async Task<ChangePasswordResult> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            var response = await _storeHubClient.ChangePasswordAsync(currentPassword, newPassword);
            if (!response.Success)
            {
                return new ChangePasswordResult(false, response.ErrorMessage);
            }

            if (response.Token is not null)
            {
                _storeHubClient.SetAuthToken(response.Token);
            }

            return new ChangePasswordResult(true, null);
        }
        catch (StoreHubClientException ex)
        {
            _logger.LogError(ex, "StoreHub connection error during change-password");
            return new ChangePasswordResult(false, "Cannot connect to StoreHub.");
        }
    }
```

Add `using IndyPOS.Application.Common.Interfaces;` if not present.

- [ ] **Step 8: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter StoreHubUserLogInServiceTests`
Expected: PASS.

- [ ] **Step 9: Commit**

```bash
git add src/IndyPOS.Application/Common/Interfaces/LogInResult.cs \
  src/IndyPOS.Application/Common/Interfaces/IUserLogInService.cs \
  src/IndyPOS.Application/Abstractions/StoreHub/IStoreHubClient.cs \
  src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubHttpClient.cs \
  src/IndyPOS.Infrastructure/Services/StoreHub/StoreHubUserLogInService.cs \
  tests/IndyPOS.Application.Tests/StoreHub/Services/StoreHubUserLogInServiceTests.cs
git commit -m "feat(winforms): defer session until rotation + client ChangePasswordAsync"
```

---

### Task C2: First-login coordinator + prompt seam

**Files:**
- Create: `src/IndyPOS.Application/Common/Interfaces/IChangePasswordPrompt.cs`
- Create: `src/IndyPOS.Application/Common/Interfaces/IFirstLoginCoordinator.cs`
- Create: `src/IndyPOS.Infrastructure/Services/StoreHub/FirstLoginCoordinator.cs`
- Modify: `src/IndyPOS.Infrastructure/ConfigureServices.cs`
- Test: `tests/IndyPOS.Application.Tests/StoreHub/Services/FirstLoginCoordinatorTests.cs`

**Interfaces:**
- Consumes: `IUserLogInService` (C1).
- Produces: `IChangePasswordPrompt.RequestNewPasswordAsync() → Task<string?>` (null = cancelled); `IFirstLoginCoordinator.LogInAsync(string,string) → Task<bool>` (ended logged in?) and `void LogOut()`.

- [ ] **Step 1: Write the interfaces**

`IChangePasswordPrompt.cs`:

```csharp
namespace IndyPOS.Application.Common.Interfaces;

/// <summary>UI seam: prompts the user for a new password. Returns null if cancelled.</summary>
public interface IChangePasswordPrompt
{
    Task<string?> RequestNewPasswordAsync();
}
```

`IFirstLoginCoordinator.cs`:

```csharp
namespace IndyPOS.Application.Common.Interfaces;

/// <summary>
/// Orchestrates login + forced first-login password rotation, keeping this
/// security-critical branching out of the coverage-excluded UI panel.
/// </summary>
public interface IFirstLoginCoordinator
{
    /// <summary>Returns true only if the user ends up fully logged in.</summary>
    Task<bool> LogInAsync(string username, string password);

    void LogOut();
}
```

- [ ] **Step 2: Write the failing tests**

`FirstLoginCoordinatorTests.cs`:

```csharp
using IndyPOS.Application.Common.Interfaces;
using IndyPOS.Infrastructure.Services.StoreHub;
using Moq;
using Xunit;

namespace IndyPOS.Application.Tests.StoreHub.Services;

public class FirstLoginCoordinatorTests
{
    private readonly Mock<IUserLogInService> _login = new();
    private readonly Mock<IChangePasswordPrompt> _prompt = new();
    private readonly FirstLoginCoordinator _sut;

    public FirstLoginCoordinatorTests()
    {
        _sut = new FirstLoginCoordinator(_login.Object, _prompt.Object);
    }

    [Fact]
    public async Task LogInAsync_NormalLogin_ReturnsTrue_NoPrompt()
    {
        _login.Setup(l => l.LogInAsync("u", "p")).ReturnsAsync(new LogInResult(true, false));

        Assert.True(await _sut.LogInAsync("u", "p"));
        _prompt.Verify(p => p.RequestNewPasswordAsync(), Times.Never);
    }

    [Fact]
    public async Task LogInAsync_FailedLogin_ReturnsFalse()
    {
        _login.Setup(l => l.LogInAsync("u", "p")).ReturnsAsync(new LogInResult(false, false));
        Assert.False(await _sut.LogInAsync("u", "p"));
    }

    [Fact]
    public async Task LogInAsync_MustChange_ChangesThenRelogins()
    {
        _login.SetupSequence(l => l.LogInAsync("u", "boot"))
              .ReturnsAsync(new LogInResult(true, true));
        _login.Setup(l => l.LogInAsync("u", "newPass123")).ReturnsAsync(new LogInResult(true, false));
        _prompt.Setup(p => p.RequestNewPasswordAsync()).ReturnsAsync("newPass123");
        _login.Setup(l => l.ChangePasswordAsync("boot", "newPass123")).ReturnsAsync(new ChangePasswordResult(true, null));

        Assert.True(await _sut.LogInAsync("u", "boot"));
        _login.Verify(l => l.LogInAsync("u", "newPass123"), Times.Once);
    }

    [Fact]
    public async Task LogInAsync_MustChange_Cancelled_LogsOut_ReturnsFalse()
    {
        _login.Setup(l => l.LogInAsync("u", "boot")).ReturnsAsync(new LogInResult(true, true));
        _prompt.Setup(p => p.RequestNewPasswordAsync()).ReturnsAsync((string?)null);

        Assert.False(await _sut.LogInAsync("u", "boot"));
        _login.Verify(l => l.LogOut(), Times.Once);
    }

    [Fact]
    public async Task LogInAsync_MustChange_ChangeFails_LogsOut_ReturnsFalse()
    {
        _login.Setup(l => l.LogInAsync("u", "boot")).ReturnsAsync(new LogInResult(true, true));
        _prompt.Setup(p => p.RequestNewPasswordAsync()).ReturnsAsync("newPass123");
        _login.Setup(l => l.ChangePasswordAsync("boot", "newPass123")).ReturnsAsync(new ChangePasswordResult(false, "bad"));

        Assert.False(await _sut.LogInAsync("u", "boot"));
        _login.Verify(l => l.LogOut(), Times.Once);
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter FirstLoginCoordinatorTests`
Expected: FAIL (type not defined).

- [ ] **Step 4: Implement the coordinator**

`FirstLoginCoordinator.cs`:

```csharp
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Infrastructure.Services.StoreHub;

public class FirstLoginCoordinator : IFirstLoginCoordinator
{
    private readonly IUserLogInService _login;
    private readonly IChangePasswordPrompt _prompt;

    public FirstLoginCoordinator(IUserLogInService login, IChangePasswordPrompt prompt)
    {
        _login = login;
        _prompt = prompt;
    }

    public async Task<bool> LogInAsync(string username, string password)
    {
        var result = await _login.LogInAsync(username, password);
        if (!result.Success)
        {
            return false;
        }

        if (!result.MustChangePassword)
        {
            return true;
        }

        var newPassword = await _prompt.RequestNewPasswordAsync();
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            _login.LogOut();
            return false;
        }

        var change = await _login.ChangePasswordAsync(password, newPassword);
        if (!change.Success)
        {
            _login.LogOut();
            return false;
        }

        // Re-login with the rotated password establishes the real session
        // (product sync + UserLoggedInEvent) via the normal path.
        var relogin = await _login.LogInAsync(username, newPassword);
        return relogin.Success;
    }

    public void LogOut() => _login.LogOut();
}
```

- [ ] **Step 5: Register in DI**

In `src/IndyPOS.Infrastructure/ConfigureServices.cs`, next to where `IUserLogInService` is registered, add (scoped/singleton to match `IUserLogInService`'s lifetime — use the same):

```csharp
        services.AddScoped<IFirstLoginCoordinator, FirstLoginCoordinator>();
```

(Use `Add<lifetime>` matching the existing `IUserLogInService` registration; if it is transient/singleton, match it.) `IChangePasswordPrompt` is registered in the WinForms layer (Task C3), not here.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test tests/IndyPOS.Application.Tests --filter FirstLoginCoordinatorTests`
Expected: PASS (5 tests).

- [ ] **Step 7: Commit**

```bash
git add src/IndyPOS.Application/Common/Interfaces/IChangePasswordPrompt.cs \
  src/IndyPOS.Application/Common/Interfaces/IFirstLoginCoordinator.cs \
  src/IndyPOS.Infrastructure/Services/StoreHub/FirstLoginCoordinator.cs \
  src/IndyPOS.Infrastructure/ConfigureServices.cs \
  tests/IndyPOS.Application.Tests/StoreHub/Services/FirstLoginCoordinatorTests.cs
git commit -m "feat(winforms): first-login coordinator + change-password prompt seam"
```

---

### Task C3: `ChangePasswordForm` + prompt impl + wire `UserLogInPanel`

**Files:**
- Create: `src/IndyPOS.Windows.Forms/UI/Login/ChangePasswordForm.cs`
- Create: `src/IndyPOS.Windows.Forms/UI/Login/ChangePasswordPrompt.cs`
- Modify: `src/IndyPOS.Windows.Forms/UI/Login/UserLogInPanel.cs`
- Modify: WinForms DI registration (locate the container setup, e.g. `Program.cs`/`Startup`/`ServiceConfiguration` in `src/IndyPOS.Windows.Forms`)

**Interfaces:**
- Consumes: `IFirstLoginCoordinator` (C2), `IChangePasswordPrompt` (C2).
- Produces: hand-coded `ChangePasswordForm` (new+confirm, ≥8, match) with `string? NewPassword`; `ChangePasswordPrompt : IChangePasswordPrompt`.

- [ ] **Step 1: Write the form (hand-coded, mirrors InstallationWizard style)**

`ChangePasswordForm.cs`:

```csharp
namespace IndyPOS.Windows.Forms.UI.Login;

/// <summary>
/// Modal shown on first login when the admin bootstrap password must be rotated.
/// Asks for a new password + confirmation only; the current (bootstrap) password
/// is supplied by the coordinator.
/// </summary>
public class ChangePasswordForm : Form
{
    private const int MinLength = 8;

    private readonly TextBox _newPassword = new() { PasswordChar = '*', Width = 260 };
    private readonly TextBox _confirmPassword = new() { PasswordChar = '*', Width = 260 };
    private readonly Label _error = new() { ForeColor = Color.Red, AutoSize = true, MaximumSize = new Size(300, 0) };

    /// <summary>The accepted new password, or null if the dialog was cancelled.</summary>
    public string? NewPassword { get; private set; }

    public ChangePasswordForm()
    {
        Text = "Set a New Password";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(340, 240);

        var prompt = new Label
        {
            Text = "You must set a new password before continuing.",
            AutoSize = true, MaximumSize = new Size(300, 0), Location = new Point(20, 15)
        };
        var newLabel = new Label { Text = "New password:", AutoSize = true, Location = new Point(20, 55) };
        _newPassword.Location = new Point(20, 78);
        var confirmLabel = new Label { Text = "Confirm password:", AutoSize = true, Location = new Point(20, 110) };
        _confirmPassword.Location = new Point(20, 133);
        _error.Location = new Point(20, 165);

        var ok = new Button { Text = "Set Password", Location = new Point(150, 200), Size = new Size(110, 30) };
        ok.Click += OnOk;
        var cancel = new Button { Text = "Cancel", Location = new Point(20, 200), Size = new Size(90, 30), DialogResult = DialogResult.Cancel };

        Controls.AddRange(new Control[]
        {
            prompt, newLabel, _newPassword, confirmLabel, _confirmPassword, _error, ok, cancel
        });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void OnOk(object? sender, EventArgs e)
    {
        var newPw = _newPassword.Text.Trim();
        var confirm = _confirmPassword.Text.Trim();

        if (newPw.Length < MinLength)
        {
            _error.Text = $"Password must be at least {MinLength} characters.";
            return;
        }
        if (newPw != confirm)
        {
            _error.Text = "Passwords do not match.";
            return;
        }

        NewPassword = newPw;
        DialogResult = DialogResult.OK;
        Close();
    }
}
```

- [ ] **Step 2: Write the prompt impl**

`ChangePasswordPrompt.cs`:

```csharp
using IndyPOS.Application.Common.Interfaces;

namespace IndyPOS.Windows.Forms.UI.Login;

/// <summary>WinForms implementation of the change-password prompt seam.</summary>
public class ChangePasswordPrompt : IChangePasswordPrompt
{
    public Task<string?> RequestNewPasswordAsync()
    {
        using var form = new ChangePasswordForm();
        var result = form.ShowDialog();
        return Task.FromResult(result == DialogResult.OK ? form.NewPassword : null);
    }
}
```

- [ ] **Step 3: Wire `UserLogInPanel` to the coordinator**

In `UserLogInPanel.cs`, change the dependency from `IUserLogInService` to `IFirstLoginCoordinator`. Replace the field, constructor, `TryLogInAsync`, and `LogOut`:

```csharp
	private readonly IFirstLoginCoordinator _coordinator;
	private readonly MessageForm _messageForm;
	private bool _isLoggedIn;

	public UserLogInPanel(IFirstLoginCoordinator coordinator,
						  MessageForm messageForm)
	{
		_coordinator = coordinator;
		_messageForm = messageForm;

		InitializeComponent();
	}
```

```csharp
	private async Task TryLogInAsync()
	{
		var username = UsersComboBox.Texts?.Trim() ?? string.Empty;
		var password = UserSecretTextBox.Texts.Trim();

		_isLoggedIn = await _coordinator.LogInAsync(username, password);

		if (_isLoggedIn)
		{
			HideUserInput();
		}
		else
		{
			_messageForm.ShowDialog("กรุณาใส่ Username และ Password ที่ถูกต้อง", "LogIn เข้าระบบไม่สำเร็จ");
		}
	}
```

```csharp
	private void LogOut()
	{
		_coordinator.LogOut();

		_isLoggedIn = false;

		ShowUserInput();
	}
```

Update the `using` at the top to include `IndyPOS.Application.Common.Interfaces` (already present).

- [ ] **Step 4: Register in WinForms DI**

Locate the WinForms DI container setup (search `src/IndyPOS.Windows.Forms` for `AddSingleton`/`AddScoped`/`AddTransient` where forms/panels are registered). Register the prompt (and confirm `UserLogInPanel` still resolves — it now needs `IFirstLoginCoordinator`, which Infrastructure registered in C2). Add:

```csharp
services.AddSingleton<IChangePasswordPrompt, ChangePasswordPrompt>();
```

(Match the lifetime convention used for other UI services in that file. Add `using IndyPOS.Application.Common.Interfaces;` and `using IndyPOS.Windows.Forms.UI.Login;` as needed.)

- [ ] **Step 5: Build**

Run: `dotnet build src/IndyPOS.Windows.Forms`
Expected: build succeeds. (Behaviour validated in the VM run, Task E2; the coordinator logic itself is unit-tested in C2.)

- [ ] **Step 6: Commit**

```bash
git add src/IndyPOS.Windows.Forms/UI/Login/ChangePasswordForm.cs \
  src/IndyPOS.Windows.Forms/UI/Login/ChangePasswordPrompt.cs \
  src/IndyPOS.Windows.Forms/UI/Login/UserLogInPanel.cs
# plus the WinForms DI file you edited
git commit -m "feat(winforms): change-password modal + wire login panel to coordinator"
```

---

## Phase D — Installer

### Task D1: Random `AdminPassword` in `InstallationConfig`

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationConfigTests.cs`

**Interfaces:**
- Produces: `InstallationConfig.AdminPassword` is a generated 14-char unambiguous string, evaluated once, no longer `required`.

- [ ] **Step 1: Write the failing test**

Add to `InstallationConfigTests.cs` (match the file's FluentAssertions style):

```csharp
    [Fact]
    public void AdminPassword_IsGenerated_StableAcrossReads_AndUnambiguous()
    {
        var config = new InstallationConfig { StoreId = "STORE-001" };

        var first = config.AdminPassword;

        first.Should().NotBeNullOrWhiteSpace();
        first.Should().HaveLength(14);
        first.IndexOfAny("0O1lI".ToCharArray()).Should().Be(-1);
        config.AdminPassword.Should().Be(first); // single evaluation, stable
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter AdminPassword_IsGenerated_StableAcrossReads_AndUnambiguous`
Expected: FAIL (currently `AdminPassword` is `required` — this won't even compile with the test; also no generator).

- [ ] **Step 3: Implement**

In `InstallationConfig.cs`, replace the `AdminPassword` property (lines 27-28) so it is generated once and not required:

```csharp
    /// <summary>
    /// Password for the initial SystemAdmin login. Generated (not user-chosen):
    /// a random, single-use BOOTSTRAP credential shown once on the finish screen
    /// and force-rotated on first login. Evaluated once so the seeded value and
    /// the displayed value never diverge.
    /// </summary>
    public string AdminPassword { get; init; } = GenerateAdminPassword();
```

Add a generator method next to `GenerateSecret` (after line 112):

```csharp
    // 14 chars from a 56-char alphabet with ambiguous glyphs (0/O/1/l/I) removed
    // so it is easy to read off the finish screen and type once. Single-use.
    private static string GenerateAdminPassword()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789";
        var chars = new char[14];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
        }
        return new string(chars);
    }
```

- [ ] **Step 4: Fix existing tests/usages that set `AdminPassword`**

Search and update any test or code that constructs `InstallationConfig` with `AdminPassword = "..."` (e.g. `DatabaseSetupTests.cs:57`). These still compile (it's now an optional `init`), but remove now-redundant literals only if they assert on the old behaviour. Run the full bootstrapper suite:

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests`
Expected: build + all tests pass (the new test included).

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/InstallationConfig.cs \
  tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationConfigTests.cs
git commit -m "feat(installer): generate random single-use admin bootstrap password"
```

---

### Task D2: Store-ID-only wizard

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs`

**Interfaces:**
- Produces: wizard collects only Store ID; `InstallationConfig` built with generated `AdminPassword`, username `"admin"`.

- [ ] **Step 1: Remove admin fields from the config panel**

In `InstallationWizard.cs`, delete the admin username/password/confirm controls and their labels/hints from `CreateConfigPanel` (lines 230-295 region): remove `_adminUsernameTextBox`, `_adminPasswordTextBox`, `_confirmPasswordTextBox`, their `Label`s (`adminUsernameLabel`, `passwordLabel`, `confirmLabel`), the `adminUsernameHint`, and drop them from `panel.Controls.AddRange`. Keep only the Store ID controls. Also remove the three field declarations (lines 29-31).

- [ ] **Step 2: Simplify validation + config build in `StartButton_Click`**

Replace the validation block + config construction (lines 310-366) with Store-ID-only validation and a generated-password config:

```csharp
        // Validate inputs
        if (string.IsNullOrWhiteSpace(_storeIdTextBox.Text))
        {
            MessageBox.Show("Please enter a Store ID.", "Validation Error",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _storeIdTextBox.Focus();
            return;
        }

        // Switch to installation view
        _configPanel.Visible = false;
        _stepLabel.Visible = true;
        _progressBar.Visible = true;
        _statusLabel.Visible = true;
        _logTextBox.Visible = true;
        _startButton.Visible = false;
        _installationStarted = true;

        var config = new InstallationConfig
        {
            StoreId = _storeIdTextBox.Text.Trim()
            // AdminUsername defaults to "admin"; AdminPassword is generated (single-use bootstrap).
            // AppPassword (database) is auto-generated by InstallationConfig.
        };
```

- [ ] **Step 3: Build**

Run: `dotnet build installer/IndyPOS.Bootstrapper`
Expected: build succeeds (no references to the removed fields remain).

- [ ] **Step 4: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs
git commit -m "feat(installer): reduce wizard to Store-ID only"
```

---

### Task D3: Surgical `RemoveInitialAdminFromConfigAsync`

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupTests.cs`

**Interfaces:**
- Produces: `DatabaseSetup.RemoveInitialAdminFromConfigAsync(CancellationToken) → Task<bool>` — removes only the `initialAdmin` node, preserves other nodes byte-identical, writes atomically, re-applies ACL. Also a static test helper `DatabaseSetup.RemoveInitialAdminNode(string json) → string`.

- [ ] **Step 1: Write the failing test**

Add to `DatabaseSetupTests.cs`:

```csharp
    [Fact]
    public void RemoveInitialAdminNode_RemovesOnlyInitialAdmin_PreservesProtectedSecrets()
    {
        var config = new InstallationConfig { StoreId = "STORE-001" };
        var original = DatabaseSetup.BuildStoreHubConfigJson(config, "jwt-secret-value");

        var stripped = DatabaseSetup.RemoveInitialAdminNode(original);

        using var doc = JsonDocument.Parse(stripped);
        var root = doc.RootElement;

        root.TryGetProperty("initialAdmin", out _).Should().BeFalse();

        // storehub-db + localToken.secretKey preserved byte-identical.
        using var originalDoc = JsonDocument.Parse(original);
        var originalConn = originalDoc.RootElement.GetProperty("connectionStrings").GetProperty("storehub-db").GetString();
        var originalKey = originalDoc.RootElement.GetProperty("localToken").GetProperty("secretKey").GetString();

        root.GetProperty("connectionStrings").GetProperty("storehub-db").GetString().Should().Be(originalConn);
        root.GetProperty("localToken").GetProperty("secretKey").GetString().Should().Be(originalKey);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter RemoveInitialAdminNode_RemovesOnlyInitialAdmin_PreservesProtectedSecrets`
Expected: FAIL (method not defined).

- [ ] **Step 3: Implement the static transform + the async file method**

In `DatabaseSetup.cs`, add `using System.Text.Json.Nodes;` at the top. Add these methods (place the static near `BuildStoreHubConfigJson`, and the async method near `CreateStoreHubConfigAsync`):

```csharp
    /// <summary>
    /// Returns <paramref name="json"/> with the top-level "initialAdmin" node removed.
    /// All other nodes (including the DPAPI-protected connection string and JWT key)
    /// are preserved exactly. Pure function for testability.
    /// </summary>
    internal static string RemoveInitialAdminNode(string json)
    {
        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidOperationException("appsettings.json is not a JSON object.");

        root.Remove("initialAdmin");

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Removes the plaintext bootstrap admin credential from appsettings.json after
    /// the admin has been seeded. Atomic (temp + move) so a crash cannot corrupt the
    /// file that the service reads on start. Re-applies the Administrators/LocalSystem ACL.
    /// Returns false (and leaves the file intact) on any failure — non-fatal.
    /// </summary>
    public async Task<bool> RemoveInitialAdminFromConfigAsync(CancellationToken cancellationToken = default)
    {
        var configPath = Path.Combine(Config.StoreHubInstallPath, "appsettings.json");

        try
        {
            if (!File.Exists(configPath))
            {
                return false;
            }

            var original = await File.ReadAllTextAsync(configPath, cancellationToken);
            var stripped = RemoveInitialAdminNode(original);

            var tempPath = configPath + ".tmp";
            await File.WriteAllTextAsync(tempPath, stripped, cancellationToken);
            File.Move(tempPath, configPath, overwrite: true);

            RestrictFilePermissions(configPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter RemoveInitialAdminNode_RemovesOnlyInitialAdmin_PreservesProtectedSecrets`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/DatabaseSetup.cs \
  tests/IndyPOS.Bootstrapper.Tests/Installers/DatabaseSetupTests.cs
git commit -m "feat(installer): surgical removal of plaintext InitialAdmin after seed"
```

---

### Task D4: Orchestrator — parse seeded marker, run clear step, return result

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs`
- Modify: `installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs`
- Test: `tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationOrchestratorTests.cs`

**Interfaces:**
- Produces: `ProvisionDatabaseResult { bool Success; string? ErrorMessage; bool AdminSeeded }` returned by `ProvisionDatabaseAsync`; `InstallationOrchestrator.InstallAsync(...) → Task<InstallationResult>` where `InstallationResult { bool AdminSeeded }`; static `StoreHubInstaller.ParseAdminSeeded(string stdout) → bool`.

- [ ] **Step 1: Write the failing marker-parse test**

Add to `InstallationOrchestratorTests.cs` (or a `StoreHubInstallerTests.cs`; match the existing style):

```csharp
    [Theory]
    [InlineData("ADMIN_SEEDED=true", true)]
    [InlineData("some log\nADMIN_SEEDED=true\nmore", true)]
    [InlineData("ADMIN_SEEDED=false", false)]
    [InlineData("no marker here", false)]
    public void ParseAdminSeeded_ReadsMarkerFromStdout(string stdout, bool expected)
    {
        StoreHubInstaller.ParseAdminSeeded(stdout).Should().Be(expected);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter ParseAdminSeeded_ReadsMarkerFromStdout`
Expected: FAIL (method not defined).

- [ ] **Step 3: Implement marker parse + capture stdout in `ProvisionDatabaseAsync`**

In `StoreHubInstaller.cs`, add the parser and a result type, and update `ProvisionDatabaseAsync` to read stdout:

```csharp
    internal static bool ParseAdminSeeded(string stdout) =>
        stdout.Contains("ADMIN_SEEDED=true", StringComparison.OrdinalIgnoreCase);
```

Change `ProvisionDatabaseAsync` to return `ProvisionDatabaseResult` and read stdout. Replace its body's process section (lines 149-164) so it reads both streams:

```csharp
            using var process = new Process { StartInfo = psi };
            process.Start();

            var stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                return new ProvisionDatabaseResult
                {
                    Success = false,
                    ErrorMessage = $"Database provisioning failed (exit {process.ExitCode}): {stderr.Trim()}"
                };
            }

            return new ProvisionDatabaseResult
            {
                Success = true,
                AdminSeeded = ParseAdminSeeded(stdout)
            };
```

Change the method signature return type to `Task<ProvisionDatabaseResult>` and update the two early-return objects (exe-not-found + catch) to `ProvisionDatabaseResult`. Add the result class near `StoreHubInstallerResult`:

```csharp
public class ProvisionDatabaseResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public bool AdminSeeded { get; init; }
}
```

- [ ] **Step 4: Wire the orchestrator — clear step + result**

In `InstallationOrchestrator.cs`:

1. Change `InstallAsync` return type to `Task<InstallationResult>`.
2. Capture the provision result (line 173) and, after the success check, add the clear step, then remember `AdminSeeded`.
3. Return `new InstallationResult { AdminSeeded = provisionResult.AdminSeeded }` at the end.

Replace lines 173-184 (the provision block) with:

```csharp
        var provisionResult = await _storeHubInstaller.ProvisionDatabaseAsync(
            new Progress<string>(msg => progress.Report(InstallationProgress.Log(msg))),
            cancellationToken);

        if (!provisionResult.Success)
        {
            throw new InstallationException($"Database provisioning failed: {provisionResult.ErrorMessage}");
        }

        progress.Report(InstallationProgress.Log("Database schema provisioned"));

        // Remove the plaintext bootstrap admin credential now that it is seeded.
        // Non-fatal: the DB is already provisioned; a failure only leaves the
        // ACL-locked plaintext behind and is retryable on re-run.
        progress.Report(InstallationProgress.Log("Removing bootstrap credential from configuration..."));
        var cleared = await _databaseSetup.RemoveInitialAdminFromConfigAsync(cancellationToken);
        progress.Report(cleared
            ? InstallationProgress.Log("Bootstrap credential removed from configuration")
            : InstallationProgress.Error("Warning: could not remove bootstrap credential from appsettings.json"));

        cancellationToken.ThrowIfCancellationRequested();
```

At the very end of `InstallAsync` (after the "Installation Complete" progress step, line 267), add:

```csharp
        return new InstallationResult { AdminSeeded = provisionResult.AdminSeeded };
```

Add the result class near `InstallationException`:

```csharp
/// <summary>Outcome of a successful installation, surfaced to the wizard's finish screen.</summary>
public class InstallationResult
{
    public bool AdminSeeded { get; init; }
}
```

- [ ] **Step 5: Run tests to verify they pass + build**

Run: `dotnet test tests/IndyPOS.Bootstrapper.Tests --filter ParseAdminSeeded_ReadsMarkerFromStdout`
Then: `dotnet build installer/IndyPOS.Bootstrapper`
Expected: test PASS; build succeeds. Fix any callers of `InstallAsync` (the wizard, Task D5) if the compiler flags the new return type — D5 handles the wizard.

- [ ] **Step 6: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/Installers/StoreHubInstaller.cs \
  installer/IndyPOS.Bootstrapper/Installers/InstallationOrchestrator.cs \
  tests/IndyPOS.Bootstrapper.Tests/Installers/InstallationOrchestratorTests.cs
git commit -m "feat(installer): clear plaintext after seed + surface AdminSeeded result"
```

---

### Task D5: Finish screen — bootstrap credential (seeded) or "retained", + summary file

**Files:**
- Modify: `installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs`

**Interfaces:**
- Consumes: `InstallationResult.AdminSeeded` (D4), `config.AdminUsername`, `config.AdminPassword` (D1).

- [ ] **Step 1: Capture the config + result at install time**

In `InstallationWizard.cs`, add a field to hold the config (so the finish screen can read the generated password). Near `_velopackExePath` (line 36):

```csharp
    private InstallationConfig? _config;
    private bool _adminSeeded;
```

In `StartButton_Click`, assign `_config = config;` right after building `config`, and capture the result of the install:

```csharp
            var result = await _orchestrator.InstallAsync(config, progress, _cts.Token);
            _adminSeeded = result.AdminSeeded;
```

- [ ] **Step 2: Show the credential (or retained message) on success**

In `StartButton_Click`, replace the two success `Log(...)` lines (currently "Installation completed successfully!" / "Click 'Finish' to launch IndyPOS.") with credential-aware output + a summary file when seeded:

```csharp
            Log("Installation completed successfully!", Color.LightGreen);

            if (_adminSeeded && _config is not null)
            {
                Log("", Color.White);
                Log("=== ADMIN SIGN-IN (save this now) ===", Color.Yellow);
                Log($"  Username: {_config.AdminUsername}", Color.White);
                Log($"  Password: {_config.AdminPassword}", Color.White);
                Log("You will be asked to set a new password on first sign-in.", Color.White);

                WriteAdminSummaryFile(_config);
            }
            else
            {
                Log("An admin account already existed — your current password is unchanged.", Color.White);
            }

            Log("Click 'Finish' to launch IndyPOS.", Color.White);
```

- [ ] **Step 3: Add the protected summary-file writer**

Add this method to `InstallationWizard` (writes an ACL-safe file under the versioned config dir):

```csharp
    private static void WriteAdminSummaryFile(InstallationConfig config)
    {
        try
        {
            var path = Path.Combine(config.ConfigDirectory, "admin-credentials.txt");
            var contents =
                "IndyPOS initial admin sign-in\r\n" +
                "================================\r\n" +
                $"Username: {config.AdminUsername}\r\n" +
                $"Password: {config.AdminPassword}\r\n\r\n" +
                "This is a one-time password. You will be required to set a new one\r\n" +
                "on first sign-in. Delete this file after you have signed in.\r\n";

            Directory.CreateDirectory(config.ConfigDirectory);
            File.WriteAllText(path, contents);
        }
        catch
        {
            // Non-fatal — the password is also shown on screen.
        }
    }
```

- [ ] **Step 4: Build**

Run: `dotnet build installer/IndyPOS.Bootstrapper`
Expected: build succeeds.

- [ ] **Step 5: Commit**

```bash
git add installer/IndyPOS.Bootstrapper/UI/InstallationWizard.cs
git commit -m "feat(installer): finish screen shows one-time admin credential + summary file"
```

---

## Phase E — Docs + full validation

### Task E1: Docs — bootstrap credential + reset-admin

**Files:**
- Modify: `docs/operations/store-installation-guide.md`

- [ ] **Step 1: Document the new flow**

Add a section describing: (1) the wizard now only asks for Store ID; (2) the finish screen shows a one-time `admin` password + writes `admin-credentials.txt` in the versioned config dir; (3) first sign-in forces a password change; (4) recovery — run the StoreHub service exe with the `reset-admin` argument to generate a fresh one-time password:

```
"%ProgramData%\IndyPOS\v4\StoreHub\IndyPOS.StoreHub.exe" reset-admin
```
It prints a new one-time password and re-arms the force-change on next sign-in.

- [ ] **Step 2: Commit**

```bash
git add docs/operations/store-installation-guide.md
git commit -m "docs(operations): admin bootstrap credential + reset-admin recovery"
```

---

### Task E2: Full build/test + installer rebuild + VM validation

**Files:** none (verification task).

- [ ] **Step 1: Full solution build**

Run: `dotnet build`
Expected: succeeds across all projects.

- [ ] **Step 2: Full unit test run**

Run: `dotnet test tests/IndyPOS.Application.Tests tests/IndyPOS.Bootstrapper.Tests`
Expected: all pass. (Integration tests `tests/IndyPOS.StoreHub.IntegrationTests` require Docker; run if available — `dotnet test tests/IndyPOS.StoreHub.IntegrationTests` — else defer to CI/VM and record the environmental skip.)

- [ ] **Step 3: Rebuild the installer**

Run: `./publish.ps1` then `./build-installer.ps1` (per the repo's existing installer build, matching prior sessions).
Expected: `publish/IndyPOS-Setup.exe` produced.

- [ ] **Step 4: VM clean-install validation (manual, `scripts/vm-testing/Reset-AndInstall.ps1 -KeepRunning`)**

Confirm each acceptance criterion:
- Wizard shows only Store ID.
- Finish screen shows `admin` + a random password; `admin-credentials.txt` exists in the versioned config dir; `appsettings.json` no longer contains `initialAdmin` (but still contains the DPAPI `storehub-db` + `localToken:secretKey`).
- First login forces the change dialog; after setting a new password, the POS opens.
- **Single-use causation matrix:** (a) after rotation, `POST /auth/login` with the old bootstrap password → 401; (b) on a separate fresh install, clear appsettings but do NOT rotate → old bootstrap still logs in (documents that rotation, not file-clearing, revokes it).
- `must_change` gate: a must-change token cannot reach `/products` (403) but can reach `/auth/change-password`.
- Re-install over the existing DB → finish screen shows "admin account already existed", not a dead credential.
- `IndyPOS.StoreHub.exe reset-admin` prints a new password that logs in and forces a change.
- In-guest DB check: `SELECT username, must_change_password FROM store_user;`.

- [ ] **Step 5: Commit any doc/status updates**

```bash
git add .claude/STATUS.md .claude/session-log.md
git commit -m "docs(indypos): admin-provisioning validated end-to-end on clean VM install"
```

---

## Self-Review

**Spec coverage:** Store-ID-only wizard (D2) ✓ · random shown-once (D1) ✓ · summary file + finish screen (D5) ✓ · surgical InitialAdmin removal preserving DPAPI secrets (D3) ✓ · seeded-vs-skipped + adaptive finish screen (D4/B6/B7) ✓ · reset-admin CLI (B6/B7) ✓ · `MustChangePassword` + migration defaultValue false (A1) ✓ · flag on envelope not StoreUserDto (A2) ✓ · must_change claim (B1) + server-side gate (B5) ✓ · change-password endpoint, identity from token, verify-without-side-effects, atomic update, fresh token, policy ≥8/≠current (B2/B3/B4) ✓ · `/auth/me` claim fix (B4) ✓ · deferred session + re-login (C1/C2) ✓ · UI thin via coordinator, testable (C2/C3) ✓ · docs (E1) ✓ · single-use causation matrix + re-install + reset + migration validation (E2) ✓.

**Placeholder scan:** No TBD/TODO; every code step has complete code; test code is concrete.

**Type consistency:** `LogInResult(Success, MustChangePassword)`, `ChangePasswordResult(Success, ErrorMessage)`, `ChangePasswordResponse(Success, Token, ErrorMessage)`, `ProvisionDatabaseResult{Success,ErrorMessage,AdminSeeded}`, `InstallationResult{AdminSeeded}`, `SetPasswordAsync(id,newHash,mustChangePassword,ct)`, `SeedAsync→Task<bool>`, `ResetAsync(newPassword,ct)` — used consistently across tasks. `must_change` claim string matches gate check. `initialAdmin` (camelCase) node name matches the serialized config.
