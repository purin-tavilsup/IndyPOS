# Epic S3: RBAC Implementation Plan

**Status:** Ready for Implementation
**Epic:** S3 - Role-Based Access Control
**Priority:** HIGH
**Created:** 2026-03-10

---

## Summary

Add capability-based RBAC to StoreHub and CloudApi. Roles map to capabilities internally for future flexibility.

**Key Decisions (from user):**
1. Keep current role names (`Cashier`, `StoreManager`, `SystemAdmin`)
2. Full admin auth for `/admin/stores/register` (SystemAdmin required)
3. Role-based with capabilities pattern (roles map to capabilities)

---

## Implementation Steps

### Step 1: Create Capability Constants

**File:** `src/IndyPOS.Application/Common/Authorization/Capability.cs` (NEW)

```csharp
namespace IndyPOS.Application.Common.Authorization;

/// <summary>
/// Defines all capabilities in the system.
/// Naming convention: {domain}.{action} (e.g., "sales.complete", "products.read")
/// </summary>
public static class Capability
{
    // Sales operations
    public const string SalesComplete = "sales.complete";

    // Product operations
    public const string ProductsRead = "products.read";

    // Sync operations
    public const string SyncViewStatus = "sync.view_status";

    // Admin operations
    public const string AdminStoresRegister = "admin.stores.register";
}
```

---

### Step 2: Create Role-to-Capability Mapping

**File:** `src/IndyPOS.Application/Common/Authorization/RoleCapabilities.cs` (NEW)

```csharp
namespace IndyPOS.Application.Common.Authorization;

using IndyPOS.Application.Common.Enums;

public static class RoleCapabilities
{
    private static readonly HashSet<string> Empty = [];

    private static readonly Dictionary<UserRole, HashSet<string>> _roleCapabilities = new()
    {
        [UserRole.Cashier] =
        [
            Capability.ProductsRead,
            Capability.SalesComplete,
        ],

        [UserRole.StoreManager] =
        [
            Capability.ProductsRead,
            Capability.SalesComplete,
            Capability.SyncViewStatus,
        ],

        [UserRole.SystemAdmin] =
        [
            Capability.ProductsRead,
            Capability.SalesComplete,
            Capability.SyncViewStatus,
            Capability.AdminStoresRegister,
        ]
    };

    /// <summary>
    /// Checks if a role has a specific capability.
    /// </summary>
    public static bool HasCapability(int roleId, string capability)
    {
        // TryGetValue handles invalid roleId by returning false
        return _roleCapabilities.TryGetValue((UserRole)roleId, out var caps)
               && caps.Contains(capability);
    }

    /// <summary>
    /// Gets all capabilities for a role.
    /// </summary>
    public static IReadOnlySet<string> GetCapabilities(UserRole role)
    {
        return _roleCapabilities.TryGetValue(role, out var caps) ? caps : Empty;
    }
}
```

---

### Step 3: Create Authorization Handler

**File:** `src/IndyPOS.Application/Common/Authorization/CapabilityAuthorizationHandler.cs` (NEW)

```csharp
namespace IndyPOS.Application.Common.Authorization;

using Microsoft.AspNetCore.Authorization;

public class CapabilityRequirement(string capability) : IAuthorizationRequirement
{
    public string Capability { get; } = capability;
}

public class CapabilityAuthorizationHandler : AuthorizationHandler<CapabilityRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CapabilityRequirement requirement)
    {
        var roleIdClaim = context.User.FindFirst("role_id")?.Value;

        if (!string.IsNullOrEmpty(roleIdClaim)
            && int.TryParse(roleIdClaim, out var roleId)
            && RoleCapabilities.HasCapability(roleId, requirement.Capability))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
```

---

### Step 4: Update StoreHub - Register Policies & Protect Endpoints

**File:** `src/IndyPOS.StoreHub/Program.cs`

**Changes at line 62** (after `builder.Services.AddAuthorization();`):

```csharp
// Add capability-based authorization
builder.Services.AddSingleton<IAuthorizationHandler, CapabilityAuthorizationHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanReadProducts", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.ProductsRead)))
    .AddPolicy("CanCompleteSales", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.SalesComplete)))
    .AddPolicy("CanViewSyncStatus", policy =>
        policy.RequireAuthenticatedUser()
              .AddRequirements(new CapabilityRequirement(Capability.SyncViewStatus)));
```

**Endpoint changes:**

| Line | Endpoint | Add |
|------|----------|-----|
| 152 | `GET /products` | `.RequireAuthorization("CanReadProducts")` |
| 169 | `POST /sales/complete` | `.RequireAuthorization("CanCompleteSales")` |
| 186 | `GET /sync/status` | `.RequireAuthorization("CanViewSyncStatus")` |

---

### Step 5: Update CloudApi - Admin Authentication

**File:** `src/IndyPOS.CloudApi/Program.cs`

**Add after existing `AddAuthorization()` (around line 40):**

```csharp
// Add StoreHub JWT validation for admin endpoints
// IMPORTANT: SecretKey must be configured - fail fast if missing
var secretKey = builder.Configuration["LocalToken:SecretKey"]
    ?? throw new InvalidOperationException("LocalToken:SecretKey configuration is required");
var issuer = builder.Configuration["LocalToken:Issuer"] ?? "indypos-storehub";
var audience = builder.Configuration["LocalToken:Audience"] ?? "indypos-clients";

builder.Services.AddAuthentication()
    .AddJwtBearer("StoreHubJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        };
    });

// Add capability handler and admin policy
builder.Services.AddSingleton<IAuthorizationHandler, CapabilityAuthorizationHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SystemAdminOnly", policy =>
    {
        policy.AuthenticationSchemes.Add("StoreHubJwt");
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new CapabilityRequirement(Capability.AdminStoresRegister));
    });
```

**Protect admin endpoint (line 273):**

```csharp
}).RequireAuthorization("SystemAdminOnly");
```

---

### Step 6: Add Unit Tests

**File:** `tests/IndyPOS.Application.Tests/Common/Authorization/RoleCapabilitiesTests.cs` (NEW)

Test cases:
- Cashier has `ProductsRead`, `SalesComplete`
- Cashier does NOT have `SyncViewStatus`, `AdminStoresRegister`
- StoreManager has `SyncViewStatus`
- SystemAdmin has all capabilities
- Invalid roleId returns false

---

## Files Summary

| File | Action | Description |
|------|--------|-------------|
| `Application/Common/Authorization/Capability.cs` | CREATE | Capability constants |
| `Application/Common/Authorization/RoleCapabilities.cs` | CREATE | Role-to-capability mapping |
| `Application/Common/Authorization/CapabilityAuthorizationHandler.cs` | CREATE | ASP.NET auth handler |
| `StoreHub/Program.cs` | MODIFY | Add policies, protect 3 endpoints |
| `CloudApi/Program.cs` | MODIFY | Add admin JWT auth, protect 1 endpoint |
| `Application.Tests/.../RoleCapabilitiesTests.cs` | CREATE | Unit tests |

---

## Endpoint Authorization Matrix

### StoreHub

| Endpoint | Before | After | Cashier | Manager | Admin |
|----------|--------|-------|:-------:|:-------:|:-----:|
| `POST /auth/login` | Public | Public | - | - | - |
| `GET /auth/me` | Auth | Auth | ✅ | ✅ | ✅ |
| `GET /products` | **None** | CanReadProducts | ✅ | ✅ | ✅ |
| `POST /sales/complete` | **None** | CanCompleteSales | ✅ | ✅ | ✅ |
| `GET /sync/status` | **None** | CanViewSyncStatus | ❌ | ✅ | ✅ |

### CloudApi

| Endpoint | Before | After | Store M2M | SystemAdmin |
|----------|--------|-------|:---------:|:-----------:|
| `POST /admin/stores/register` | **None** | SystemAdminOnly | ❌ | ✅ |
| Other protected endpoints | Auth | Auth (unchanged) | ✅ | ✅ |

---

## Test Scenarios

1. **Cashier GET /products** → 200 OK ✅
2. **Cashier POST /sales/complete** → 200 OK ✅
3. **Cashier GET /sync/status** → 403 Forbidden ❌
4. **Manager GET /sync/status** → 200 OK ✅
5. **Unauthenticated GET /products** → 401 Unauthorized
6. **SystemAdmin POST /admin/stores/register** → 200 OK ✅
7. **Manager POST /admin/stores/register** → 403 Forbidden ❌
8. **Store M2M token POST /admin/stores/register** → 403 Forbidden ❌

---

## Future Extensibility

This design enables:
- Add capabilities to `Capability.cs` without touching endpoints
- Add roles to enum and `RoleCapabilities` mapping
- Split capabilities (e.g., `Sales.Create`, `Sales.Void`)
- Easy audit logging in authorization handler

---

## Design Decisions & Rationale

### D1: Keep Current Role Names
**Decision:** Keep `Cashier`, `StoreManager`, `SystemAdmin` instead of aligning to spec's `Cashier`, `Manager`, `Owner`, `Admin`.

**Rationale:**
- Current system behavior is essentially "Cashier" vs "Not Cashier" - no real permission differences yet
- `StoreManager → Manager` is just a rename, but `SystemAdmin → Owner + Admin` introduces new permission boundaries the code doesn't model yet
- Renaming without real permission differences = label churn with no security gain
- Cleaner to do a proper RBAC migration later when we actually need `Owner` vs `Admin` distinction
- Avoids DB migration, UI changes, and spec/code mismatch during transition

### D2: Full Admin Authentication (Not API Key)
**Decision:** Protect `/admin/stores/register` with user JWT auth requiring `SystemAdmin` role.

**Rationale:**
- This is a high-impact provisioning endpoint (creates stores, affects tenancy)
- Shared API key has ugly failure modes:
  - One leaked key = full access
  - No user identity for audit trail
  - Hard to rotate safely
  - Hard to scope permissions later
- User authentication provides:
  - Identity (who registered the store)
  - Auditability
  - Per-user revocation
  - Easier path to richer RBAC

**Alternative rejected:** `X-Admin-Key` header - too flimsy for production, only acceptable for strictly temporary internal-only endpoints.

### D3: Capability-Based RBAC (Roles Map to Capabilities)
**Decision:** Use `HasCapability(roleId, capability)` pattern instead of direct role checks.

**Rationale:**
- Direct role checks (`if role == Manager`) scatter authorization logic across codebase
- Capability pattern provides:
  - Single source of truth (`RoleCapabilities.cs`)
  - Decoupling - roles can change without touching endpoint code
  - Testability - strongly typed, not magic strings
  - Clear permission matrix for documentation
- Still simple (3 roles, ~4 capabilities to start)
- Easy migration path to fine-grained permissions later if needed

**Alternative rejected:** Fine-grained permission strings (`sales.complete`, `sales.void`, etc.) - overkill early, adds permission tables, seeding, claim mapping complexity. Not needed until we have custom per-user permissions or multi-tenant policy differences.

### D4: Shared JWT Signing Key for CloudApi Admin Auth
**Decision:** CloudApi validates StoreHub JWTs for admin endpoints using shared secret key.

**Rationale:**
- Admin users already log in via StoreHub (local POS)
- Reusing StoreHub JWT avoids building separate admin login flow
- Simpler than adding ASP.NET Identity right now (that's S4)
- Shared key is acceptable for single-deployment scenario (same operator controls both services)

**Trade-off:** Requires keeping signing keys in sync between StoreHub and CloudApi config.

**Future:** S4 (ASP.NET Identity) will add proper CloudUser authentication; this is the MVP path.

### D5: Authorization Handler in Application Layer
**Decision:** Place `CapabilityAuthorizationHandler` in `Application/Common/Authorization/`.

**Rationale:**
- Capabilities are a business concept, not infrastructure
- `RoleCapabilities` mapping is business logic
- Application layer can be referenced by both StoreHub and CloudApi
- Keeps authorization logic testable without ASP.NET Core dependencies (mostly)

**Trade-off:** `IAuthorizationHandler` is from `Microsoft.AspNetCore.Authorization`, so this adds a small ASP.NET dependency to Application layer. Acceptable because authorization is inherently tied to the web host.

---

## Scope Boundaries

**In Scope (S3):**
- Capability constants and role mapping
- Authorization policies for StoreHub endpoints
- Admin JWT auth for CloudApi
- Unit tests for RoleCapabilities

**Out of Scope (Future):**
- S4: ASP.NET Identity integration (CloudUser management)
- S5: RSA key signing (replace symmetric keys)
- S7: Security audit logging (log authorization decisions)
- S8: Rate limiting
- Fine-grained per-user permissions
- Owner vs Admin role distinction

---

## Team Review Feedback (Incorporated)

| Feedback | Resolution |
|----------|------------|
| Add `RequireAuthenticatedUser()` to policies | ✅ Added to all policies |
| Remove `Enum.IsDefined` from hot path | ✅ Simplified - TryGetValue handles invalid roles |
| Add `GetCapabilities(role)` helper | ✅ Added for cleaner handler |
| JWT signing key default is unsafe | ✅ Changed to `throw` if missing |
| Log authorization failures | 📝 Noted for S7 (audit logging) |
| Typed capabilities vs strings | 📝 Kept strings for now, consider later |

### Future Capability Considerations

Typical POS managers may also need:
- `products.write` - Product management
- `inventory.adjust` - Inventory adjustments
- `sales.void` - Void/refund sales
- `reports.daily` - Daily reports access

These can be added to `Capability.cs` and `RoleCapabilities.cs` when needed.

### Naming Convention

```
Capability naming pattern: {domain}.{action}

Examples:
- sales.complete
- products.read
- admin.stores.register
- sync.view_status
```
