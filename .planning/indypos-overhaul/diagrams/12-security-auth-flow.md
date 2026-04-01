# Security & Authentication Flow

Version: 1.0.0
Date: 2026-03-31
Status: ✅ Epic S1-S5 Complete

## Overview

This diagram documents the authentication and authorization architecture for IndyPOS, covering:
- POS offline authentication (StoreHub)
- Cloud API OAuth2 authentication (OpenIddict)
- Role-Based Access Control (RBAC) with capabilities

---

## Authentication Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTHENTICATION ARCHITECTURE                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           STORE (LOCAL)                                      │
│                                                                             │
│  ┌──────────────────┐         ┌──────────────────────────────────────┐     │
│  │  POS Desktop     │         │         StoreHub API                  │     │
│  │  (Windows.Forms) │         │                                       │     │
│  │                  │         │  ┌─────────────────────────────────┐ │     │
│  │  Login Form      │──POST──►│  │  POST /auth/login               │ │     │
│  │  • Username      │  /auth/ │  │                                 │ │     │
│  │  • Password      │  login  │  │  1. Validate credentials        │ │     │
│  │                  │         │  │  2. BCrypt password verify      │ │     │
│  │  ┌────────────┐  │◄───────│  │  3. Generate JWT token          │ │     │
│  │  │ JWT Token  │  │  JWT   │  │  4. Return token + user info    │ │     │
│  │  │ (cached)   │  │        │  └─────────────────────────────────┘ │     │
│  │  └────────────┘  │         │                                       │     │
│  │                  │         │  User Store:                          │     │
│  │  Subsequent      │         │  ┌─────────────────────────────────┐ │     │
│  │  API calls       │──────► │  │  PostgreSQL (local)              │ │     │
│  │  include         │ Bearer │  │  • Users table                   │ │     │
│  │  Authorization   │ Token  │  │  • BCrypt hashed passwords       │ │     │
│  │  header          │         │  │  • Roles (Cashier, Manager...)   │ │     │
│  └──────────────────┘         │  └─────────────────────────────────┘ │     │
│                               └──────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                           CLOUD                                              │
│                                                                             │
│  ┌──────────────────┐         ┌──────────────────────────────────────┐     │
│  │  StoreHub        │         │         CloudApi                      │     │
│  │  (SyncWorker)    │         │                                       │     │
│  │                  │         │  ┌─────────────────────────────────┐ │     │
│  │  OAuth2 Client   │──POST──►│  │  POST /oauth/token               │ │     │
│  │  Credentials     │  /oauth │  │  (OpenIddict)                    │ │     │
│  │  • ClientId      │  /token │  │                                 │ │     │
│  │  • ClientSecret  │         │  │  1. Validate client credentials │ │     │
│  │                  │         │  │  2. Check scopes                │ │     │
│  │  ┌────────────┐  │◄───────│  │  3. Sign JWT with RSA key       │ │     │
│  │  │ JWT Token  │  │  JWT   │  │  4. Return access_token         │ │     │
│  │  │ (cached)   │  │ (15min)│  └─────────────────────────────────┘ │     │
│  │  └────────────┘  │         │                                       │     │
│  │                  │         │  Protected Endpoints:                 │     │
│  │  Sync Events     │──────► │  • POST /sync/events [Authorize]    │     │
│  │  to Cloud        │ Bearer │  • GET /master/products [Authorize]  │     │
│  │                  │ Token  │  • GET /admin/users [Authorize]      │     │
│  └──────────────────┘         └──────────────────────────────────────┘     │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## StoreHub Authentication Flow (Offline-Capable)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    POS LOGIN FLOW (Offline-Capable)                          │
└─────────────────────────────────────────────────────────────────────────────┘

User                    WinForms                 StoreHub API           Database
 │                         │                          │                     │
 │  Enter credentials      │                          │                     │
 │ ───────────────────────►│                          │                     │
 │                         │                          │                     │
 │                         │  POST /auth/login        │                     │
 │                         │  {username, password}    │                     │
 │                         │─────────────────────────►│                     │
 │                         │                          │                     │
 │                         │                          │  SELECT user        │
 │                         │                          │  WHERE username=?   │
 │                         │                          │────────────────────►│
 │                         │                          │                     │
 │                         │                          │◄────────────────────│
 │                         │                          │  User + hash        │
 │                         │                          │                     │
 │                         │                          │  BCrypt.Verify      │
 │                         │                          │  (password, hash)   │
 │                         │                          │                     │
 │                         │                          │  Generate JWT       │
 │                         │                          │  • sub: userId      │
 │                         │                          │  • role: Cashier    │
 │                         │                          │  • exp: +8 hours    │
 │                         │                          │                     │
 │                         │◄─────────────────────────│                     │
 │                         │  {token, user}           │                     │
 │                         │                          │                     │
 │                         │  Cache token in memory   │                     │
 │                         │  (IStoreHubClient)       │                     │
 │                         │                          │                     │
 │◄────────────────────────│                          │                     │
 │  Login success          │                          │                     │
 │                         │                          │                     │

Offline Capability:
───────────────────
• Users cached locally in PostgreSQL (synced from cloud)
• BCrypt password hashes stored locally
• No internet required for login
• JWT tokens validated locally
```

---

## CloudApi OAuth2 Flow (OpenIddict)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    OAUTH2 CLIENT CREDENTIALS FLOW                            │
└─────────────────────────────────────────────────────────────────────────────┘

StoreHub                                          CloudApi
(SyncWorker)                                     (OpenIddict)
    │                                                 │
    │  POST /oauth/token                              │
    │  Content-Type: application/x-www-form-urlencoded│
    │  grant_type=client_credentials                  │
    │  client_id=store_1                              │
    │  client_secret=<secret>                         │
    │  scope=sync.write master.read                   │
    │ ───────────────────────────────────────────────►│
    │                                                 │
    │                                                 │  Validate client
    │                                                 │  • Check client_id exists
    │                                                 │  • BCrypt verify secret
    │                                                 │  • Check allowed scopes
    │                                                 │
    │                                                 │  Generate JWT
    │                                                 │  • Sign with RSA key
    │                                                 │  • Include scopes as claims
    │                                                 │  • Set 15-minute expiry
    │                                                 │
    │◄───────────────────────────────────────────────│
    │  {                                              │
    │    "access_token": "eyJ...",                    │
    │    "token_type": "Bearer",                      │
    │    "expires_in": 900                            │
    │  }                                              │
    │                                                 │
    │  Cache token locally                            │
    │  (CloudTokenService)                            │
    │                                                 │
    │  POST /sync/events                              │
    │  Authorization: Bearer eyJ...                   │
    │ ───────────────────────────────────────────────►│
    │                                                 │
    │                                                 │  Validate JWT
    │                                                 │  • Verify RSA signature
    │                                                 │  • Check expiry
    │                                                 │  • Check scope claims
    │                                                 │
    │◄───────────────────────────────────────────────│
    │  200 OK                                         │
    │                                                 │


Token Caching (CloudTokenService):
───────────────────────────────────
┌──────────────────────────────────────────┐
│  In-Memory Token Cache                   │
│                                          │
│  • Thread-safe (SemaphoreSlim)           │
│  • 30-second expiry buffer               │
│  • Auto-refresh before expiry            │
│  • Graceful offline degradation          │
└──────────────────────────────────────────┘
```

---

## Store Registration Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    STORE REGISTRATION FLOW                                   │
└─────────────────────────────────────────────────────────────────────────────┘

Admin                    CloudApi                   OpenIddict DB
  │                         │                            │
  │  POST /admin/stores/register                         │
  │  Authorization: Bearer <admin_token>                 │
  │  { "storeId": 1 }       │                            │
  │────────────────────────►│                            │
  │                         │                            │
  │                         │  Generate credentials      │
  │                         │  ClientId: store_1         │
  │                         │  Secret: <32-byte random>  │
  │                         │                            │
  │                         │  INSERT INTO               │
  │                         │  OpenIddictApplications    │
  │                         │  (ClientId, SecretHash,    │
  │                         │   Permissions)             │
  │                         │────────────────────────────►│
  │                         │                            │
  │                         │◄────────────────────────────│
  │                         │  OK                        │
  │                         │                            │
  │◄────────────────────────│                            │
  │  {                      │                            │
  │    "clientId": "store_1",                            │
  │    "clientSecret": "abc123..."                       │
  │  }                      │                            │
  │                         │                            │

⚠️ IMPORTANT: clientSecret shown only once!
   Must be stored securely (DPAPI on Windows)
```

---

## RBAC: Role-Based Access Control

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CAPABILITY-BASED RBAC                                     │
└─────────────────────────────────────────────────────────────────────────────┘

Roles → Capabilities → Policies → Endpoints

┌─────────────────┐    ┌─────────────────────┐    ┌─────────────────────────┐
│     ROLES       │    │    CAPABILITIES     │    │      ENDPOINTS          │
├─────────────────┤    ├─────────────────────┤    ├─────────────────────────┤
│                 │    │                     │    │                         │
│  Cashier        │───►│  products.read      │───►│  GET /products          │
│                 │───►│  sales.complete     │───►│  POST /sales/complete   │
│                 │    │                     │    │                         │
├─────────────────┤    ├─────────────────────┤    ├─────────────────────────┤
│                 │    │                     │    │                         │
│  StoreManager   │───►│  products.read      │───►│  GET /products          │
│                 │───►│  sales.complete     │───►│  POST /sales/complete   │
│                 │───►│  products.manage    │───►│  POST/PUT/DEL /products │
│                 │───►│  inventory.adjust   │───►│  POST /adjust-quantity  │
│                 │───►│  reports.view       │───►│  GET /reports/*         │
│                 │───►│  sync.view          │───►│  GET /sync/status       │
│                 │    │                     │    │                         │
├─────────────────┤    ├─────────────────────┤    ├─────────────────────────┤
│                 │    │                     │    │                         │
│  SystemAdmin    │───►│  (all capabilities) │───►│  (all endpoints)        │
│                 │───►│  users.manage       │───►│  CRUD /admin/users      │
│                 │───►│  stores.manage      │───►│  POST /admin/stores/*   │
│                 │    │                     │    │                         │
└─────────────────┘    └─────────────────────┘    └─────────────────────────┘
```

---

## Capability Matrix

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    CAPABILITY MATRIX                                         │
├──────────────────────┬─────────┬──────────────┬─────────────┬───────────────┤
│ Capability           │ Cashier │ StoreManager │ SystemAdmin │ Policy Name   │
├──────────────────────┼─────────┼──────────────┼─────────────┼───────────────┤
│ products.read        │   ✅    │      ✅      │     ✅      │ CanReadProducts│
│ sales.complete       │   ✅    │      ✅      │     ✅      │ CanCompleteSales│
│ products.manage      │   ❌    │      ✅      │     ✅      │ CanManageProducts│
│ inventory.adjust     │   ❌    │      ✅      │     ✅      │ CanAdjustInventory│
│ reports.view         │   ❌    │      ✅      │     ✅      │ CanViewReports │
│ sync.view            │   ❌    │      ✅      │     ✅      │ CanViewSyncStatus│
│ users.read           │   ❌    │      ❌      │     ✅      │ CanReadUsers   │
│ users.create         │   ❌    │      ❌      │     ✅      │ CanCreateUsers │
│ users.update         │   ❌    │      ❌      │     ✅      │ CanUpdateUsers │
│ users.deactivate     │   ❌    │      ❌      │     ✅      │ CanDeactivateUsers│
│ stores.manage        │   ❌    │      ❌      │     ✅      │ SystemAdminOnly│
└──────────────────────┴─────────┴──────────────┴─────────────┴───────────────┘
```

---

## Authorization Handler Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    AUTHORIZATION HANDLER FLOW                                │
└─────────────────────────────────────────────────────────────────────────────┘

Request                  Middleware                   Handler              Action
   │                         │                           │                    │
   │  GET /reports/sales     │                           │                    │
   │  Authorization: Bearer  │                           │                    │
   │ ────────────────────────►                           │                    │
   │                         │                           │                    │
   │                         │  1. JWT Validation        │                    │
   │                         │  • Verify signature       │                    │
   │                         │  • Check expiry           │                    │
   │                         │  • Extract claims         │                    │
   │                         │                           │                    │
   │                         │  [Authorize("CanViewReports")]                 │
   │                         │ ─────────────────────────►│                    │
   │                         │                           │                    │
   │                         │                           │  2. Get user role  │
   │                         │                           │  role = "Cashier"  │
   │                         │                           │                    │
   │                         │                           │  3. Check capability
   │                         │                           │  RoleCapabilities  │
   │                         │                           │   .HasCapability(  │
   │                         │                           │     "Cashier",     │
   │                         │                           │     "reports.view")│
   │                         │                           │     = false        │
   │                         │                           │                    │
   │                         │◄─────────────────────────│                    │
   │                         │  AuthorizationResult.Fail │                    │
   │                         │                           │                    │
   │◄────────────────────────│                           │                    │
   │  403 Forbidden          │                           │                    │
   │                         │                           │                    │


Code Structure:
───────────────
Application/
└── Common/
    └── Authorization/
        ├── Capability.cs              ← Capability constants
        ├── RoleCapabilities.cs        ← Role-to-capability mapping
        └── CapabilityAuthorizationHandler.cs  ← ASP.NET handler
```

---

## Secret Storage

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SECRET STORAGE ARCHITECTURE                               │
└─────────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────┐    ┌─────────────────────────────────────┐
│  CloudApi (Linux/Cloud)         │    │  StoreHub (Windows/Local)           │
├─────────────────────────────────┤    ├─────────────────────────────────────┤
│                                 │    │                                     │
│  RSA Signing Key                │    │  Client Secret Storage              │
│                                 │    │                                     │
│  • Environment variable:        │    │  • DPAPI (Windows Data Protection)  │
│    INDYPOS_RSA_SIGNING_KEY      │    │  • LocalMachine scope               │
│                                 │    │  • %ProgramData%\IndyPOS\Secrets\   │
│  • PEM format (Base64)          │    │                                     │
│  • RSA 2048+ bits               │    │  ┌─────────────────────────────┐   │
│                                 │    │  │  ISecretStorage             │   │
│  ┌─────────────────────────┐   │    │  │  • StoreAsync(key, value)   │   │
│  │  OpenIddictExtensions   │   │    │  │  • RetrieveAsync(key)       │   │
│  │                         │   │    │  │  • DeleteAsync(key)         │   │
│  │  .AddSigningKey(        │   │    │  └─────────────────────────────┘   │
│  │    RSA.ImportFromPem()) │   │    │              │                      │
│  └─────────────────────────┘   │    │              ▼                      │
│                                 │    │  ┌─────────────────────────────┐   │
│  Fallback (dev only):           │    │  │  DpapiSecretStorage         │   │
│  • AddDevelopmentSigningCert()  │    │  │  (Infrastructure)           │   │
│                                 │    │  └─────────────────────────────┘   │
└─────────────────────────────────┘    └─────────────────────────────────────┘

Key Rotation Support (S6 - Not Started):
────────────────────────────────────────
• KeyId generated from SHA256 of public key
• Multiple keys supported (add new, deprecate old)
• 6-month rotation recommended
```

---

## JWT Token Structure

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    JWT TOKEN STRUCTURE                                       │
└─────────────────────────────────────────────────────────────────────────────┘

StoreHub JWT (POS Users):
─────────────────────────
{
  "header": {
    "alg": "HS256",
    "typ": "JWT"
  },
  "payload": {
    "sub": "f47ac10b-58cc-4372-a567-0e02b2c3d479",  // User GUID
    "name": "สมชาย",                                 // Display name
    "role": "StoreManager",                          // User role
    "store_id": 1,                                   // Store identifier
    "iat": 1711929600,                              // Issued at
    "exp": 1711958400                               // Expires (8 hours)
  }
}


CloudApi JWT (Store Machines):
──────────────────────────────
{
  "header": {
    "alg": "RS256",                                  // RSA signature
    "typ": "at+jwt",                                 // Access token
    "kid": "abc123"                                  // Key ID (for rotation)
  },
  "payload": {
    "iss": "https://cloud.indypos.com",             // Issuer
    "aud": "indypos-cloud",                         // Audience
    "client_id": "store_1",                         // Store client
    "scope": "sync.write master.read",              // Granted scopes
    "iat": 1711929600,
    "exp": 1711930500                               // 15 minutes
  }
}
```

---

## Security Implementation Status

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    SECURITY IMPLEMENTATION STATUS                            │
├────────────────────────────────────┬────────┬───────────────────────────────┤
│ Feature                            │ Status │ Notes                         │
├────────────────────────────────────┼────────┼───────────────────────────────┤
│ S1: POS Offline Authentication     │   ✅   │ BCrypt, JWT, local users      │
│ S2: Local User Cache               │   ✅   │ Sync from cloud, cache locally│
│ S3: RBAC Implementation            │   ✅   │ Capability-based              │
│ S4: CloudApi User Management       │   ✅   │ Admin CRUD endpoints          │
│ S5: RSA Signing + DPAPI Secrets    │   ✅   │ Env vars + DPAPI              │
│ S6: Key Rotation                   │   🔴   │ LOW priority                  │
│ S7: Security Audit Logging         │   🔴   │ LOW priority                  │
│ S8: Rate Limiting                  │   🔴   │ LOW priority                  │
│ S9: Secrets Management             │   🔴   │ Covered by S5                 │
└────────────────────────────────────┴────────┴───────────────────────────────┘
```

---

## Threat Mitigations

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    THREAT MITIGATIONS                                        │
├────────────────────────────────┬────────────┬───────────────────────────────┤
│ Threat                         │ Status     │ Mitigation                    │
├────────────────────────────────┼────────────┼───────────────────────────────┤
│ Stolen POS device              │  🟡 Partial│ DPAPI secrets, local auth    │
│ Credential theft               │  🟡 Partial│ BCrypt hashing (rate limit TODO)│
│ API abuse                      │  🟡 Partial│ JWT validation (rate limit TODO)│
│ Insider misuse                 │  ✅ Done   │ RBAC capabilities             │
│ Token theft                    │  ✅ Done   │ Short expiry (15min cloud)    │
│ Secret exposure                │  ✅ Done   │ DPAPI + env vars              │
│ Replay attacks                 │  ✅ Done   │ JWT expiry + one-time use     │
└────────────────────────────────┴────────────┴───────────────────────────────┘
```

---

**Related:**
- `01-architecture-overview.md` - System architecture
- `05-sync-flow-outbox-pattern.md` - Sync flow details
- `.planning/indypos-overhaul/security/indypos_security_design_spec.md` - Full spec
